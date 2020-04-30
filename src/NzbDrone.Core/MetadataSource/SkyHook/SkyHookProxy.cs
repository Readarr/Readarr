using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.Music;
using NzbDrone.Core.Profiles.Metadata;

namespace NzbDrone.Core.MetadataSource.SkyHook
{
    public class SkyHookProxy : IProvideAuthorInfo, ISearchForNewAuthor, IProvideBookInfo, ISearchForNewBook, ISearchForNewEntity
    {
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;
        private readonly IArtistService _authorService;
        private readonly IAlbumService _bookService;
        private readonly IMetadataRequestBuilder _requestBuilder;
        private readonly IMetadataProfileService _metadataProfileService;
        private readonly ICached<HashSet<string>> _cache;

        public SkyHookProxy(IHttpClient httpClient,
                            IMetadataRequestBuilder requestBuilder,
                            IArtistService authorService,
                            IAlbumService albumService,
                            Logger logger,
                            IMetadataProfileService metadataProfileService,
                            ICacheManager cacheManager)
        {
            _httpClient = httpClient;
            _metadataProfileService = metadataProfileService;
            _requestBuilder = requestBuilder;
            _authorService = authorService;
            _bookService = albumService;
            _cache = cacheManager.GetCache<HashSet<string>>(GetType());
            _logger = logger;
        }

        public HashSet<string> GetChangedArtists(DateTime startTime)
        {
            return null;
        }

        public Author GetAuthorInfo(string foreignAuthorId)
        {
            _logger.Debug("Getting Author details ReadarrAPI.MetadataID of {0}", foreignAuthorId);

            var httpRequest = _requestBuilder.GetRequestBuilder().Create()
                .SetSegment("route", $"author/{foreignAuthorId}")
                .Build();

            httpRequest.AllowAutoRedirect = true;
            httpRequest.SuppressHttpError = true;

            var httpResponse = _httpClient.Get<AuthorResource>(httpRequest);

            if (httpResponse.HasHttpError)
            {
                if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new ArtistNotFoundException(foreignAuthorId);
                }
                else if (httpResponse.StatusCode == HttpStatusCode.BadRequest)
                {
                    throw new BadRequestException(foreignAuthorId);
                }
                else
                {
                    throw new HttpException(httpRequest, httpResponse);
                }
            }

            return MapAuthor(httpResponse.Resource);
        }

        public HashSet<string> GetChangedAlbums(DateTime startTime)
        {
            return _cache.Get("ChangedAlbums", () => GetChangedAlbumsUncached(startTime), TimeSpan.FromMinutes(30));
        }

        private HashSet<string> GetChangedAlbumsUncached(DateTime startTime)
        {
            return null;
        }

        public Tuple<string, Book, List<AuthorMetadata>> GetBookInfo(string foreignBookId)
        {
            _logger.Debug("Getting Book with ReadarrAPI.MetadataID of {0}", foreignBookId);

            var httpRequest = _requestBuilder.GetRequestBuilder().Create()
                                             .SetSegment("route", $"book/{foreignBookId}")
                                             .Build();

            httpRequest.AllowAutoRedirect = true;
            httpRequest.SuppressHttpError = true;

            var httpResponse = _httpClient.Get<BookResource>(httpRequest);

            if (httpResponse.HasHttpError)
            {
                if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new ArtistNotFoundException(foreignBookId);
                }
                else if (httpResponse.StatusCode == HttpStatusCode.BadRequest)
                {
                    throw new BadRequestException(foreignBookId);
                }
                else
                {
                    throw new HttpException(httpRequest, httpResponse);
                }
            }

            var b = httpResponse.Resource;
            var book = MapBook(b);

            var authors = httpResponse.Resource.AuthorMetadata.SelectList(MapAuthor);
            var authorid = GetAuthorId(b);
            book.AuthorMetadata = authors.First(x => x.ForeignAuthorId == authorid);

            return new Tuple<string, Book, List<AuthorMetadata>>(authorid, book, authors);
        }

        public List<Author> SearchForNewAuthor(string title)
        {
            var books = SearchForNewBook(title, null);

            return books.Select(x => x.Author.Value).ToList();
        }

        public List<Book> SearchForNewBook(string title, string artist)
        {
            try
            {
                var lowerTitle = title.ToLowerInvariant();

                var split = lowerTitle.Split(':');
                var prefix = split[0];

                if (split.Length == 2 && new[] { "readarr", "readarrid", "goodreads", "isbn", "asin" }.Contains(prefix))
                {
                    var slug = split[1].Trim();

                    if (slug.IsNullOrWhiteSpace() || slug.Any(char.IsWhiteSpace))
                    {
                        return new List<Book>();
                    }

                    if (prefix == "goodreads" || prefix == "readarr" || prefix == "readarrid")
                    {
                        var isValid = int.TryParse(slug, out var searchId);
                        if (!isValid)
                        {
                            return new List<Book>();
                        }

                        return SearchByGoodreadsId(searchId);
                    }
                    else if (prefix == "isbn")
                    {
                        return SearchByIsbn(slug);
                    }
                    else if (prefix == "asin")
                    {
                        return SearchByAsin(slug);
                    }
                }

                var q = title.ToLower().Trim();
                if (artist != null)
                {
                    q += " " + artist;
                }

                var httpRequest = _requestBuilder.GetRequestBuilder().Create()
                    .SetSegment("route", "search")
                    .AddQueryParam("q", q)
                    .Build();

                var result = _httpClient.Get<BookSearchResource>(httpRequest);

                return MapSearchResult(result.Resource);
            }
            catch (HttpException)
            {
                throw new SkyHookException("Search for '{0}' failed. Unable to communicate with ReadarrAPI.", title);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, ex.Message);
                throw new SkyHookException("Search for '{0}' failed. Invalid response received from ReadarrAPI.", title);
            }
        }

        public List<Book> SearchByIsbn(string isbn)
        {
            return SearchByAlternateId("isbn", isbn);
        }

        public List<Book> SearchByAsin(string asin)
        {
            return SearchByAlternateId("asin", asin);
        }

        public List<Book> SearchByGoodreadsId(int goodreadsId)
        {
            return SearchByAlternateId("goodreads", goodreadsId.ToString());
        }

        private List<Book> SearchByAlternateId(string type, string id)
        {
            try
            {
                var httpRequest = _requestBuilder.GetRequestBuilder().Create()
                    .SetSegment("route", $"book/{type}/{id}")
                    .Build();

                var httpResponse = _httpClient.Get<BookSearchResource>(httpRequest);

                var result = _httpClient.Get<BookSearchResource>(httpRequest);

                return MapSearchResult(result.Resource);
            }
            catch (HttpException)
            {
                throw new SkyHookException("Search for {0} '{1}' failed. Unable to communicate with ReadarrAPI.", type, id);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, ex.Message);
                throw new SkyHookException("Search for {0 }'{1}' failed. Invalid response received from ReadarrAPI.", type, id);
            }
        }

        public List<Book> SearchForNewAlbumByRecordingIds(List<string> recordingIds)
        {
            return null;
        }

        public List<object> SearchForNewEntity(string title)
        {
            var books = SearchForNewBook(title, null);

            var result = new List<object>();
            foreach (var book in books)
            {
                var author = book.Author.Value;

                if (!result.Contains(author))
                {
                    result.Add(author);
                }

                result.Add(book);
            }

            return result;
        }

        private Author MapAuthor(AuthorResource resource)
        {
            var metadata = MapAuthor(resource.AuthorMetadata.First(x => x.ForeignId == resource.ForeignId));

            var allBooks = resource.Books
                .Where(x => GetAuthorId(x) == resource.ForeignId)
                .Select(MapBook)
                .ToList();

            allBooks.ForEach(x => x.AuthorMetadata = metadata);

            var bookDict = allBooks.ToDictionary(x => x.ForeignBookId);

            var seriesList = new List<Series>();

            // only take series where there are some works
            foreach (var seriesResource in resource.Series.Where(x => x.BookLinks.Any()))
            {
                var series = MapSeries(seriesResource);
                series.LinkItems = new List<SeriesBookLink>();
                foreach (var item in seriesResource.BookLinks.Where(x => bookDict.ContainsKey(x.BookForeignId)))
                {
                    series.LinkItems.Value.Add(new SeriesBookLink
                    {
                        Position = item.Position,
                        Book = bookDict[item.BookForeignId],
                        IsPrimary = item.Primary
                    });
                }

                seriesList.Add(series);
            }

            var result = new Author
            {
                Metadata = metadata,
                CleanName = Parser.Parser.CleanArtistName(metadata.Name),
                SortName = Parser.Parser.NormalizeTitle(metadata.Name),
                Books = allBooks,
                Series = seriesList
            };

            return result;
        }

        private static AuthorMetadata MapAuthor(AuthorSummaryResource resource)
        {
            var author = new AuthorMetadata
            {
                ForeignAuthorId = resource.ForeignId,
                GoodreadsId = resource.GoodreadsId,
                TitleSlug = resource.TitleSlug,
                Name = resource.Name.CleanSpaces(),
                Overview = resource.Description,
                Ratings = new Ratings { Votes = resource.RatingsCount, Value = (decimal)resource.AverageRating }
            };

            author.Images.Add(new MediaCover.MediaCover
            {
                Url = resource.ImageUrl,
                CoverType = MediaCoverTypes.Poster
            });

            author.Links.Add(new Links { Url = resource.WebUrl, Name = "Goodreads" });

            return author;
        }

        private static Series MapSeries(SeriesResource resource)
        {
            var series = new Series
            {
                ForeignSeriesId = resource.ForeignId,
                Title = resource.Title,
                Description = resource.Description
            };

            return series;
        }

        private static Book MapBook(BookResource resource)
        {
            var book = new Book
            {
                ForeignBookId = resource.ForeignId,
                ForeignWorkId = resource.WorkForeignId,
                GoodreadsId = resource.GoodreadsId,
                TitleSlug = resource.TitleSlug,
                Isbn13 = resource.Isbn13,
                Asin = resource.Asin,
                Title = resource.Title.CleanSpaces(),
                Language = resource.Language,
                Publisher = resource.Publisher,
                CleanTitle = Parser.Parser.CleanArtistName(resource.Title),
                Overview = resource.Description,
                ReleaseDate = resource.ReleaseDate,
                Ratings = new Ratings { Votes = resource.RatingCount, Value = (decimal)resource.AverageRating }
            };

            book.Images.Add(new MediaCover.MediaCover { Url = resource.ImageUrl, CoverType = MediaCoverTypes.Cover });

            book.Links.Add(new Links { Url = resource.WebUrl, Name = "Goodreads" });

            return book;
        }

        private List<Book> MapSearchResult(BookSearchResource resource)
        {
            var metadata = resource.AuthorMetadata.SelectList(MapAuthor).ToDictionary(x => x.ForeignAuthorId);

            var result = new List<Book>();

            foreach (var b in resource.Books)
            {
                var book = _bookService.FindById(b.ForeignId);
                if (book == null)
                {
                    book = MapBook(b);

                    var authorid = GetAuthorId(b);

                    if (authorid == null)
                    {
                        continue;
                    }

                    var author = _authorService.FindById(authorid);

                    if (author == null)
                    {
                        var authorMetadata = metadata[authorid];

                        author = new Author
                        {
                            CleanName = Parser.Parser.CleanArtistName(authorMetadata.Name),
                            Metadata = authorMetadata
                        };
                    }

                    book.Author = author;
                    book.AuthorMetadata = author.Metadata.Value;
                }

                result.Add(book);
            }

            return result;
        }

        private string GetAuthorId(BookResource b)
        {
            return b.Contributors.FirstOrDefault()?.ForeignId;
        }
    }
}
