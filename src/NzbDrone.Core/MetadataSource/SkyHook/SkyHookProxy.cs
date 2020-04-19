using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.Music;
using NzbDrone.Core.Profiles.Metadata;

namespace NzbDrone.Core.MetadataSource.SkyHook
{
    public class SkyHookProxy : IProvideAuthorInfo, ISearchForNewAuthor, IProvideBookInfo, ISearchForNewBook, ISearchForNewEntity
    {
        private static readonly Regex PartOrSetRegex = new Regex(@"(?:\d+ of \d+|\d+/\d+|(?<from>\d+)-(?<to>\d+))");

        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;
        private readonly IAlbumService _bookService;
        private readonly IMetadataRequestBuilder _requestBuilder;
        private readonly IMetadataProfileService _metadataProfileService;
        private readonly ICached<HashSet<string>> _cache;

        public SkyHookProxy(IHttpClient httpClient,
                            IMetadataRequestBuilder requestBuilder,
                            IAlbumService albumService,
                            Logger logger,
                            IMetadataProfileService metadataProfileService,
                            ICacheManager cacheManager)
        {
            _httpClient = httpClient;
            _metadataProfileService = metadataProfileService;
            _requestBuilder = requestBuilder;
            _bookService = albumService;
            _cache = cacheManager.GetCache<HashSet<string>>(GetType());
            _logger = logger;
        }

        public HashSet<string> GetChangedArtists(DateTime startTime)
        {
            return null;
        }

        public Author GetAuthorInfo(string foreignAuthorId, int metadataProfileId)
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

            return MapAuthor(httpResponse.Resource, metadataProfileId);
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
            var authorid = b.Contributors.First(x => x.Role == "Author").ForeignId;
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

                if (lowerTitle.StartsWith("readarr:") || lowerTitle.StartsWith("readarrid:") || lowerTitle.StartsWith("goodreads:"))
                {
                    var slug = lowerTitle.Split(':')[1].Trim();

                    var isValid = long.TryParse(slug, out var searchId);

                    if (slug.IsNullOrWhiteSpace() || slug.Any(char.IsWhiteSpace) || isValid == false)
                    {
                        return new List<Book>();
                    }

                    try
                    {
                        var existingBook = _bookService.FindById(searchId.ToString());
                        if (existingBook != null)
                        {
                            return new List<Book> { existingBook };
                        }

                        return new List<Book> { GetBookInfo(searchId.ToString()).Item2 };
                    }
                    catch (ArtistNotFoundException)
                    {
                        return new List<Book>();
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
            return null;
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

        private Author MapAuthor(AuthorResource resource, int metadataProfileId)
        {
            var metadata = MapAuthor(resource.AuthorMetadata.First(x => x.ForeignId == resource.ForeignId));

            var allBooks = resource.Books
                .Where(x => x.Contributors.FirstOrDefault(c => c.Role == "Author")?.ForeignId == resource.ForeignId)
                .Select(MapBook);

            var seriesLinks = resource.Series.SelectMany(x => x.BookLinks)
                .GroupBy(x => x.BookForeignId)
                .ToDictionary(x => x.Key, y => y.ToList());

            var books = FilterBooks(allBooks, seriesLinks, metadataProfileId);

            books.ForEach(x => x.AuthorMetadata = metadata);

            var bookDict = books.ToDictionary(x => x.ForeignBookId);

            // remove works that we've filtered out
            foreach (var s in resource.Series)
            {
                s.BookLinks = s.BookLinks.Where(x => bookDict.ContainsKey(x.BookForeignId)).ToList();
            }

            var seriesList = new List<Series>();

            // only take series where there are some works
            foreach (var seriesResource in resource.Series.Where(x => x.BookLinks.Any()))
            {
                var series = MapSeries(seriesResource);
                series.LinkItems = new List<SeriesBookLink>();
                foreach (var item in seriesResource.BookLinks)
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

            var bookSeriesLinks = seriesList.SelectMany(x => x.LinkItems.Value)
                .GroupBy(x => x.Book.Value.ForeignBookId)
                .ToDictionary(x => x.Key, y => y.ToList());

            foreach (var book in books)
            {
                if (bookSeriesLinks.TryGetValue(book.ForeignBookId, out var links))
                {
                    book.SeriesLinks = links;
                }
            }

            var result = new Author
            {
                Metadata = metadata,
                CleanName = Parser.Parser.CleanArtistName(metadata.Name),
                SortName = Parser.Parser.NormalizeTitle(metadata.Name),
                Books = books,
                Series = seriesList
            };

            return result;
        }

        private List<Book> FilterBooks(IEnumerable<Book> books, Dictionary<string, List<SeriesBookLinkResource>> seriesLinks, int metadataProfileId)
        {
            var p = _metadataProfileService.Get(metadataProfileId);
            var allowedLanguages = new HashSet<string>(p.AllowedLanguages?.Split(',') ?? Enumerable.Empty<string>());

            var result = books
                .Where(x => x.Ratings.Votes >= p.MinRatingCount &&
                       (double)x.Ratings.Value >= p.MinRating &&
                       (!p.SkipMissingDate || x.ReleaseDate.HasValue) &&
                       (!p.SkipMissingIsbn || x.Isbn13.IsNotNullOrWhiteSpace() || x.Asin.IsNotNullOrWhiteSpace()) &&
                       (!p.SkipPartsAndSets || !IsPartOrSet(x, seriesLinks.GetValueOrDefault(x.ForeignBookId))) &&
                       (!p.SkipSeriesSecondary || !seriesLinks.ContainsKey(x.ForeignBookId) || seriesLinks[x.ForeignBookId].Any(y => y.Primary)) &&
                       (!allowedLanguages.Any() || allowedLanguages.Contains(x.Language)))
                .ToList();

            return result;
        }

        private bool IsPartOrSet(Book book, List<SeriesBookLinkResource> seriesLinks)
        {
            if (seriesLinks != null && !seriesLinks.Any(s => double.TryParse(s.Position, out _)))
            {
                // No series entries parse to a number, so all like 1-3 etc.
                return true;
            }

            var match = PartOrSetRegex.Match(book.Title);

            if (match.Groups["from"].Success)
            {
                var from = int.Parse(match.Groups["from"].Value);
                return from >= 1800 && from <= DateTime.UtcNow.Year ? false : true;
            }

            return match.Success;
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
                Language = "eng",
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
                    var authorid = b.Contributors.FirstOrDefault(x => x.Role == "Author")?.ForeignId;

                    if (authorid == null)
                    {
                        continue;
                    }

                    var author = metadata[authorid];

                    book = MapBook(b);
                    book.Author = new Author
                    {
                        CleanName = Parser.Parser.CleanArtistName(author.Name),
                        Metadata = author
                    };
                    book.AuthorMetadata = author;
                }

                result.Add(book);
            }

            return result;
        }
    }
}
