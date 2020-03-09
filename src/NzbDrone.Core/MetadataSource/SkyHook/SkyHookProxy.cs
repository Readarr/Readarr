using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Profiles.Metadata;

namespace NzbDrone.Core.MetadataSource.SkyHook
{
    public class SkyHookProxy : IProvideAuthorInfo, ISearchForNewAuthor, IProvideBookInfo, ISearchForNewBook, ISearchForNewEntity
    {
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
                .SetSegment("route", $"author/show/{foreignAuthorId}.xml")
                .AddQueryParam("exclude_books", "true")
                .Build();

            httpRequest.AllowAutoRedirect = true;
            httpRequest.SuppressHttpError = true;

            var httpResponse = _httpClient.Get(httpRequest);

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

            var resource = httpResponse.Deserialize<AuthorResource>();
            var author = new Author();
            author.Metadata = MapAuthor(resource);
            author.CleanName = Parser.Parser.CleanArtistName(author.Metadata.Value.Name);
            author.SortName = Parser.Parser.NormalizeTitle(author.Metadata.Value.Name);

            author.Books = GetAuthorBooks(foreignAuthorId, metadataProfileId);

            return author;
        }

        private List<Book> GetAuthorBooks(string foreignAuthorId, int metadataProfileId)
        {
            _logger.Debug("Getting Author Books with ReadarrAPI.MetadataID of {0}", foreignAuthorId);

            var httpRequest = _requestBuilder.GetRequestBuilder().Create()
                .SetSegment("route", $"author/list/{foreignAuthorId}.xml")
                .AddQueryParam("per_page", 100000)
                .Build();

            httpRequest.AllowAutoRedirect = true;
            httpRequest.SuppressHttpError = true;

            var httpResponse = _httpClient.Get(httpRequest);

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

            var resource = httpResponse.Deserialize<AuthorBookListResource>();

            var allBooks = resource.List
                .Where(x => x.Authors.First().Id.ToString() == foreignAuthorId)
                .Select(MapBook);

            var metadataProfile = _metadataProfileService.Get(metadataProfileId);

            var books = resource.List.Where(x => x.Authors.First().Id.ToString() == foreignAuthorId)
                .Select(MapBook)
                .Where(x => x.Ratings.Votes >= metadataProfile.MinRatingCount && (double)x.Ratings.Value >= metadataProfile.MinRating)
                .ToList();

            books.ForEach(x => x.CleanTitle = x.Title.CleanArtistName());

            return books;
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
                                             .SetSegment("route", $"book/show/{foreignBookId}.xml")
                                             .Build();

            httpRequest.AllowAutoRedirect = true;
            httpRequest.SuppressHttpError = true;

            var httpResponse = _httpClient.Get(httpRequest);

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

            var resource = httpResponse.Deserialize<BookResource>();

            var book = MapBook(resource);
            book.CleanTitle = Parser.Parser.CleanArtistName(book.Title);

            var authors = resource.Authors.SelectList(MapAuthor);
            book.AuthorMetadata = authors.First();

            return new Tuple<string, Book, List<AuthorMetadata>>(resource.Authors.First().Id.ToString(), book, authors);
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
                    .SetSegment("route", "search.xml")
                    .AddQueryParam("page", 1)
                    .AddQueryParam("per_page", 20)
                    .AddQueryParam("search[field]", "all")
                    .AddQueryParam("q", q)
                    .Build();

                var httpResponse = _httpClient.Get(httpRequest);
                var result = httpResponse.Deserialize<BookSearchResultResource>();

                return result.Results.SelectList(MapSearchResult);
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
            try
            {
                var httpRequest = _requestBuilder.GetRequestBuilder().Create()
                    .SetSegment("route", "search.xml")
                    .AddQueryParam("page", 1)
                    .AddQueryParam("per_page", 20)
                    .AddQueryParam("search[field]", "isbn")
                    .AddQueryParam("q", isbn)
                    .Build();

                var httpResponse = _httpClient.Get(httpRequest);
                var result = httpResponse.Deserialize<BookSearchResultResource>();

                return result.Results?.SelectList(MapSearchResult);
            }
            catch (HttpException)
            {
                throw new SkyHookException("Search for isbn '{0}' failed. Unable to communicate with ReadarrAPI.", isbn);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, ex.Message);
                throw new SkyHookException("Search for isbn '{0}' failed. Invalid response received from ReadarrAPI.", isbn);
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

        private static AuthorMetadata MapAuthor(AuthorResource resource)
        {
            var author = new AuthorMetadata
            {
                ForeignAuthorId = resource.Id.ToString(),
                Name = resource.Name.CleanSpaces(),
                Overview = resource.About
            };

            author.Images.Add(new MediaCover.MediaCover
            {
                Url = resource.LargeImageUrl,
                CoverType = MediaCoverTypes.Poster
            });

            author.Links.Add(new Links { Url = resource.Link, Name = "Goodreads" });

            return author;
        }

        private static AuthorMetadata MapAuthor(AuthorSummaryResource resource)
        {
            var author = new AuthorMetadata
            {
                ForeignAuthorId = resource.Id.ToString(),
                Name = resource.Name.CleanSpaces()
            };

            author.Images.Add(new MediaCover.MediaCover
            {
                Url = resource.ImageUrl,
                CoverType = MediaCoverTypes.Poster
            });

            return author;
        }

        private static Book MapBook(BookResource resource)
        {
            var book = new Book
            {
                ForeignBookId = resource.Id.ToString(),
                Isbn13 = resource.Isbn13,
                Asin = resource.Asin ?? resource.KindleAsin,
                Title = (resource.Work.OriginalTitle ?? resource.Title).CleanSpaces(),
                Overview = resource.Description,
                ReleaseDate = resource.Work.OriginalPublicationDate ?? resource.PublicationDate,
                Ratings = MapRatings(resource)
            };

            book.Images.Add(new MediaCover.MediaCover { Url = resource.ImageUrl, CoverType = MediaCoverTypes.Cover });

            if (resource.BookLinks != null)
            {
                book.Links.AddRange(resource.BookLinks.Select(x => new Links { Url = x.Link, Name = x.Name }));
            }

            if (resource.BuyLinks != null)
            {
                book.Links.AddRange(resource.BuyLinks.Select(x => new Links { Url = x.Link, Name = x.Name }));
            }

            return book;
        }

        private static Ratings MapRatings(BookResource resource)
        {
            if (resource.Work.RatingsCount > 0)
            {
                return MapRatings(resource.Work);
            }

            return new Ratings { Votes = resource.RatingsCount, Value = resource.AverageRating };
        }

        private static Ratings MapRatings(WorkResource resource)
        {
            return new Ratings { Votes = resource.RatingsCount, Value = resource.AverageRating };
        }

        private Book MapSearchResult(WorkResource resource)
        {
            var book = _bookService.FindById(resource.BestBookId.ToString());
            if (book == null && resource.BestBook != null)
            {
                book = new Book();
                book.ForeignBookId = resource.BestBook.Id.ToString();
                book.Title = resource.BestBook.Title;
                book.ReleaseDate = resource.OriginalPublicationDate;
                book.Images.Add(new MediaCover.MediaCover { Url = resource.BestBook.ImageUrl, CoverType = MediaCoverTypes.Cover });
                book.Ratings = MapRatings(resource);
                book.Author = new Author
                {
                    CleanName = Parser.Parser.CleanArtistName(resource.BestBook.AuthorName),
                    Metadata = new AuthorMetadata()
                    {
                        ForeignAuthorId = resource.BestBook.AuthorId.ToString(),
                        Name = resource.BestBook.AuthorName,
                        Ratings = new Ratings()
                    }
                };

                book.AuthorMetadata = book.Author.Value.Metadata.Value;
                book.CleanTitle = book.Title.CleanArtistName();
            }

            return book;
        }
    }
}
