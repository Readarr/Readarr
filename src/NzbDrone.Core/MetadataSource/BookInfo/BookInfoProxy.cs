using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Books;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource.Goodreads;

namespace NzbDrone.Core.MetadataSource.BookInfo
{
    public class BookInfoProxy : IProvideAuthorInfo, IProvideBookInfo, ISearchForNewBook, ISearchForNewAuthor, ISearchForNewEntity
    {
        private readonly IHttpClient _httpClient;
        private readonly IGoodreadsSearchProxy _goodreadsSearchProxy;
        private readonly IAuthorService _authorService;
        private readonly IBookService _bookService;
        private readonly IEditionService _editionService;
        private readonly Logger _logger;
        private readonly IMetadataRequestBuilder _requestBuilder;
        private readonly ICached<HashSet<string>> _cache;

        public BookInfoProxy(IHttpClient httpClient,
                             IGoodreadsSearchProxy goodreadsSearchProxy,
                             IAuthorService authorService,
                             IBookService bookService,
                             IEditionService editionService,
                             IMetadataRequestBuilder requestBuilder,
                             Logger logger,
                             ICacheManager cacheManager)
        {
            _httpClient = httpClient;
            _goodreadsSearchProxy = goodreadsSearchProxy;
            _authorService = authorService;
            _bookService = bookService;
            _editionService = editionService;
            _requestBuilder = requestBuilder;
            _cache = cacheManager.GetCache<HashSet<string>>(GetType());
            _logger = logger;
        }

        public HashSet<string> GetChangedAuthors(DateTime startTime)
        {
            var httpRequest = _requestBuilder.GetRequestBuilder().Create()
                .SetSegment("route", "author/changed")
                .AddQueryParam("since", startTime.ToString("o"))
                .Build();

            httpRequest.SuppressHttpError = true;

            var httpResponse = _httpClient.Get<RecentUpdatesResource>(httpRequest);

            if (httpResponse.Resource.Limited)
            {
                return null;
            }

            return new HashSet<string>(httpResponse.Resource.Ids.Select(x => x.ToString()));
        }

        public Author GetAuthorInfo(string foreignAuthorId, bool useCache = false, bool includeBooks = true)
        {
            _logger.Debug("Getting Author details GoodreadsId of {0}", foreignAuthorId);

            return PollAuthor(foreignAuthorId, includeBooks);
        }

        private Author PollAuthor(string foreignAuthorId, bool includeBooks)
        {
            AuthorResource resource = null;

            for (var i = 0; i < 60; i++)
            {
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
                        throw new AuthorNotFoundException(foreignAuthorId);
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

                resource = httpResponse.Resource;

                if (resource.Works != null || !includeBooks)
                {
                    resource.Works ??= new List<WorkResource>();
                    resource.Series ??= new List<SeriesResource>();
                    break;
                }

                Thread.Sleep(2000);
            }

            if (resource?.Works == null)
            {
                throw new BookInfoException($"Failed to get works for {foreignAuthorId}");
            }

            return MapAuthor(resource);
        }

        public Author GetAuthorAndBooks(string foreignAuthorId, double minPopularity = 0)
        {
            return GetAuthorInfo(foreignAuthorId);
        }

        public HashSet<string> GetChangedBooks(DateTime startTime)
        {
            return _cache.Get("ChangedBooks", () => GetChangedBooksUncached(startTime), TimeSpan.FromMinutes(30));
        }

        private HashSet<string> GetChangedBooksUncached(DateTime startTime)
        {
            return null;
        }

        public Tuple<string, Book, List<AuthorMetadata>> GetBookInfo(string foreignBookId, bool useCache = false)
        {
            return PollBook(foreignBookId);
        }

        public List<Author> SearchForNewAuthor(string title)
        {
            var books = SearchForNewBook(title, null);

            return books.Select(x => x.Author.Value).ToList();
        }

        public List<Book> SearchForNewBook(string title, string author)
        {
            var q = title.ToLower().Trim();
            if (author != null)
            {
                q += " " + author;
            }

            try
            {
                var lowerTitle = title.ToLowerInvariant();

                var split = lowerTitle.Split(':');
                var prefix = split[0];

                if (split.Length == 2 && new[] { "author", "work", "edition", "isbn", "asin" }.Contains(prefix))
                {
                    var slug = split[1].Trim();

                    if (slug.IsNullOrWhiteSpace() || slug.Any(char.IsWhiteSpace))
                    {
                        return new List<Book>();
                    }

                    if (prefix == "author" || prefix == "work" || prefix == "edition")
                    {
                        var isValid = int.TryParse(slug, out var searchId);
                        if (!isValid)
                        {
                            return new List<Book>();
                        }

                        if (prefix == "author")
                        {
                            return SearchByGoodreadsAuthorId(searchId);
                        }

                        if (prefix == "work")
                        {
                            return SearchByGoodreadsWorkId(searchId);
                        }

                        if (prefix == "edition")
                        {
                            return SearchByGoodreadsBookId(searchId);
                        }
                    }

                    q = slug;
                }

                return Search(q);
            }
            catch (HttpException ex)
            {
                _logger.Warn(ex, ex.Message);
                throw new GoodreadsException("Search for '{0}' failed. Unable to communicate with Goodreads.", title);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, ex.Message);
                throw new GoodreadsException("Search for '{0}' failed. Invalid response received from Goodreads.",
                    title);
            }
        }

        public List<Book> SearchByIsbn(string isbn)
        {
            return Search(isbn);
        }

        public List<Book> SearchByAsin(string asin)
        {
            return Search(asin);
        }

        private List<Book> SearchByGoodreadsAuthorId(int id)
        {
            try
            {
                var authorId = id.ToString();
                var result = GetAuthorInfo(authorId);
                var books = result.Books.Value.OrderByDescending(x => x.Ratings.Popularity).Take(10).ToList();
                var authors = new Dictionary<string, AuthorMetadata> { { authorId, result.Metadata.Value } };

                foreach (var book in books)
                {
                    AddDbIds(authorId, book, authors);
                }

                return books;
            }
            catch (AuthorNotFoundException)
            {
                return new List<Book>();
            }
        }

        public List<Book> SearchByGoodreadsWorkId(int id)
        {
            try
            {
                var tuple = GetBookInfo(id.ToString());
                AddDbIds(tuple.Item1, tuple.Item2, tuple.Item3.ToDictionary(x => x.ForeignAuthorId));
                return new List<Book> { tuple.Item2 };
            }
            catch (BookNotFoundException)
            {
                return new List<Book>();
            }
        }

        public List<Book> SearchByGoodreadsBookId(int id)
        {
            var httpRequest = _requestBuilder.GetRequestBuilder().Create()
                .SetSegment("route", $"book/{id}")
                .Build();

            httpRequest.SuppressHttpError = true;

            var httpResponse = _httpClient.Get<BulkBookResource>(httpRequest);

            return MapBulkBook(httpResponse.Resource);
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

        private List<Book> Search(string query)
        {
            var result = _goodreadsSearchProxy.Search(query);

            var ids = result.Select(x => x.BookId).ToList();

            return MapSearchResult(ids);
        }

        private List<Book> MapSearchResult(List<int> ids)
        {
            var httpRequest = _requestBuilder.GetRequestBuilder().Create()
                .SetSegment("route", $"book/bulk")
                .SetHeader("Content-Type", "application/json")
                .Build();

            httpRequest.SetContent(ids.ToJson());

            httpRequest.AllowAutoRedirect = true;

            var httpResponse = _httpClient.Post<BulkBookResource>(httpRequest);

            var mapped = MapBulkBook(httpResponse.Resource);

            var idStr = ids.Select(x => x.ToString()).ToList();

            return mapped.OrderBy(b => idStr.IndexOf(b.Editions.Value.First().ForeignEditionId)).ToList();
        }

        private List<Book> MapBulkBook(BulkBookResource resource)
        {
            var authors = resource.Authors.Select(MapAuthorMetadata).ToDictionary(x => x.ForeignAuthorId, x => x);

            var series = resource.Series.Select(MapSeries).ToList();

            var books = new List<Book>();

            foreach (var work in resource.Works)
            {
                var book = MapBook(work);
                var authorId = work.Books.OrderByDescending(b => b.AverageRating * b.RatingCount).First().Contributors.First().ForeignId.ToString();

                AddDbIds(authorId, book, authors);

                books.Add(book);
            }

            MapSeriesLinks(series, books, resource.Series);

            return books;
        }

        private void AddDbIds(string authorId, Book book, Dictionary<string, AuthorMetadata> authors)
        {
            var dbBook = _bookService.FindById(book.ForeignBookId);
            if (dbBook != null)
            {
                book.UseDbFieldsFrom(dbBook);

                var editions = _editionService.GetEditionsByBook(dbBook.Id).ToDictionary(x => x.ForeignEditionId);
                foreach (var edition in book.Editions.Value)
                {
                    if (editions.TryGetValue(edition.ForeignEditionId, out var dbEdition))
                    {
                        edition.UseDbFieldsFrom(dbEdition);
                    }
                }
            }

            var author = _authorService.FindById(authorId);

            if (author == null)
            {
                var metadata = authors[authorId];

                author = new Author
                {
                    CleanName = Parser.Parser.CleanAuthorName(metadata.Name),
                    Metadata = metadata
                };
            }

            book.Author = author;
            book.AuthorMetadata = author.Metadata.Value;
            book.AuthorMetadataId = author.AuthorMetadataId;
        }

        private Tuple<string, Book, List<AuthorMetadata>> PollBook(string foreignBookId)
        {
            WorkResource resource = null;

            for (var i = 0; i < 60; i++)
            {
                var httpRequest = _requestBuilder.GetRequestBuilder().Create()
                    .SetSegment("route", $"work/{foreignBookId}")
                    .Build();

                httpRequest.AllowAutoRedirect = true;
                httpRequest.SuppressHttpError = true;

                var httpResponse = _httpClient.Get<WorkResource>(httpRequest);

                if (httpResponse.HasHttpError)
                {
                    if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                    {
                        throw new BookNotFoundException(foreignBookId);
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

                resource = httpResponse.Resource;

                if (resource.Books != null)
                {
                    break;
                }

                Thread.Sleep(2000);
            }

            if (resource?.Books == null)
            {
                throw new BookInfoException($"Failed to get books for {foreignBookId}");
            }

            var book = MapBook(resource);
            var authorId = resource.Books.OrderByDescending(x => x.AverageRating * x.RatingCount).First().Contributors.First().ForeignId.ToString();
            var metadata = resource.Authors.Select(MapAuthorMetadata).ToList();

            var series = resource.Series.Select(MapSeries).ToList();
            MapSeriesLinks(series, new List<Book> { book }, resource.Series);

            return Tuple.Create(authorId, book, metadata);
        }

        private static AuthorMetadata MapAuthorMetadata(AuthorResource resource)
        {
            var metadata = new AuthorMetadata
            {
                ForeignAuthorId = resource.ForeignId.ToString(),
                TitleSlug = resource.ForeignId.ToString(),
                Name = resource.Name.CleanSpaces(),
                Overview = resource.Description,
                Ratings = new Ratings { Votes = resource.RatingCount, Value = (decimal)resource.AverageRating },
                Status = AuthorStatusType.Continuing
            };

            metadata.SortName = metadata.Name.ToLower();
            metadata.NameLastFirst = metadata.Name.ToLastFirst();
            metadata.SortNameLastFirst = metadata.NameLastFirst.ToLower();

            if (resource.ImageUrl.IsNotNullOrWhiteSpace())
            {
                metadata.Images.Add(new MediaCover.MediaCover
                {
                    Url = resource.ImageUrl,
                    CoverType = MediaCoverTypes.Poster
                });
            }

            if (resource.Url.IsNotNullOrWhiteSpace())
            {
                metadata.Links.Add(new Links { Url = resource.Url, Name = "Goodreads" });
            }

            return metadata;
        }

        private static Author MapAuthor(AuthorResource resource)
        {
            var metadata = MapAuthorMetadata(resource);

            var books = resource.Works
                .Where(x => x.ForeignId > 0 && GetAuthorId(x) == resource.ForeignId)
                .Select(MapBook)
                .ToList();

            books.ForEach(x => x.AuthorMetadata = metadata);

            var series = resource.Series.Select(MapSeries).ToList();

            MapSeriesLinks(series, books, resource.Series);

            var result = new Author
            {
                Metadata = metadata,
                CleanName = Parser.Parser.CleanAuthorName(metadata.Name),
                Books = books,
                Series = series
            };

            return result;
        }

        private static void MapSeriesLinks(List<Series> series, List<Book> books, List<SeriesResource> resource)
        {
            var bookDict = books.ToDictionary(x => x.ForeignBookId);
            var seriesDict = series.ToDictionary(x => x.ForeignSeriesId);

            foreach (var book in books)
            {
                book.SeriesLinks = new List<SeriesBookLink>();
            }

            // only take series where there are some works
            foreach (var s in resource.Where(x => x.LinkItems.Any()))
            {
                if (seriesDict.TryGetValue(s.ForeignId.ToString(), out var curr))
                {
                    curr.LinkItems = s.LinkItems.Where(x => x.ForeignWorkId.IsNotNullOrWhiteSpace() && bookDict.ContainsKey(x.ForeignWorkId.ToString())).Select(l => new SeriesBookLink
                    {
                        Book = bookDict[l.ForeignWorkId.ToString()],
                        Series = curr,
                        IsPrimary = l.Primary,
                        Position = l.PositionInSeries,
                        SeriesPosition = l.SeriesPosition
                    }).ToList();

                    foreach (var l in curr.LinkItems.Value)
                    {
                        l.Book.Value.SeriesLinks.Value.Add(l);
                    }
                }
            }
        }

        private static Series MapSeries(SeriesResource resource)
        {
            var series = new Series
            {
                ForeignSeriesId = resource.ForeignId.ToString(),
                Title = resource.Title,
                Description = resource.Description
            };

            return series;
        }

        private static Book MapBook(WorkResource resource)
        {
            var book = new Book
            {
                ForeignBookId = resource.ForeignId.ToString(),
                Title = resource.Title,
                TitleSlug = resource.ForeignId.ToString(),
                CleanTitle = Parser.Parser.CleanAuthorName(resource.Title),
                ReleaseDate = resource.ReleaseDate,
                Genres = resource.Genres,
                RelatedBooks = resource.RelatedWorks
            };

            book.Links.Add(new Links { Url = resource.Url, Name = "Goodreads Editions" });

            if (resource.Books != null)
            {
                book.Editions = resource.Books.Select(x => MapEdition(x)).ToList();

                // monitor the most popular release
                var mostPopular = book.Editions.Value.OrderByDescending(x => x.Ratings.Popularity).FirstOrDefault();
                if (mostPopular != null)
                {
                    mostPopular.Monitored = true;

                    // fix work title if missing
                    if (book.Title.IsNullOrWhiteSpace())
                    {
                        book.Title = mostPopular.Title;
                    }
                }
            }
            else
            {
                book.Editions = new List<Edition>();
            }

            // If we are missing the book release date, set as the earliest edition release date
            if (!book.ReleaseDate.HasValue)
            {
                var editionReleases = book.Editions.Value
                    .Where(x => x.ReleaseDate.HasValue && x.ReleaseDate.Value.Month != 1 && x.ReleaseDate.Value.Day != 1)
                    .ToList();

                if (editionReleases.Any())
                {
                    book.ReleaseDate = editionReleases.Min(x => x.ReleaseDate.Value);
                }
                else
                {
                    editionReleases = book.Editions.Value.Where(x => x.ReleaseDate.HasValue).ToList();
                    if (editionReleases.Any())
                    {
                        book.ReleaseDate = editionReleases.Min(x => x.ReleaseDate.Value);
                    }
                }
            }

            Debug.Assert(!book.Editions.Value.Any() || book.Editions.Value.Count(x => x.Monitored) == 1, "one edition monitored");

            book.AnyEditionOk = true;

            var ratingCount = book.Editions.Value.Sum(x => x.Ratings.Votes);

            if (ratingCount > 0)
            {
                book.Ratings = new Ratings
                {
                    Votes = ratingCount,
                    Value = book.Editions.Value.Sum(x => x.Ratings.Votes * x.Ratings.Value) / ratingCount
                };
            }
            else
            {
                book.Ratings = new Ratings { Votes = 0, Value = 0 };
            }

            return book;
        }

        private static Edition MapEdition(BookResource resource)
        {
            var edition = new Edition
            {
                ForeignEditionId = resource.ForeignId.ToString(),
                TitleSlug = resource.ForeignId.ToString(),
                Isbn13 = resource.Isbn13,
                Asin = resource.Asin,
                Title = resource.Title.CleanSpaces(),
                Language = resource.Language,
                Overview = resource.Description,
                Format = resource.Format,
                IsEbook = resource.IsEbook,
                Disambiguation = resource.EditionInformation,
                Publisher = resource.Publisher,
                PageCount = resource.NumPages ?? 0,
                ReleaseDate = resource.ReleaseDate,
                Ratings = new Ratings { Votes = resource.RatingCount, Value = (decimal)resource.AverageRating }
            };

            if (resource.ImageUrl.IsNotNullOrWhiteSpace())
            {
                edition.Images.Add(new MediaCover.MediaCover
                {
                    Url = resource.ImageUrl,
                    CoverType = MediaCoverTypes.Cover
                });
            }

            edition.Links.Add(new Links { Url = resource.Url, Name = "Goodreads Book" });

            return edition;
        }

        private static int GetAuthorId(WorkResource b)
        {
            return b.Books.First().Contributors.FirstOrDefault()?.ForeignId ?? 0;
        }
    }
}
