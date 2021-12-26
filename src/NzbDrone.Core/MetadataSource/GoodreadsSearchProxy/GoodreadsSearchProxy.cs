using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Books;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Http;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.MetadataSource.Goodreads
{
    public class GoodreadsSearchProxy : ISearchForNewAuthor, ISearchForNewBook, ISearchForNewEntity
    {
        private static readonly RegexReplace FullSizeImageRegex = new RegexReplace(@"\._[SU][XY]\d+_.jpg$",
            ".jpg",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex DuplicateSpacesRegex = new Regex(@"\s{2,}", RegexOptions.Compiled);

        private static readonly Regex NoPhotoRegex = new Regex(@"/nophoto/(book|user)/",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly List<Regex> SeriesRegex = new List<Regex>
        {
            new Regex(@"\((?<series>[^,]+),\s+#(?<position>[\w\d\.]+)\)$", RegexOptions.Compiled),
            new Regex(@"(The\s+(?<series>.+)\s+Series\s+Book\s+(?<position>[\w\d\.]+)\)$)", RegexOptions.Compiled)
        };

        private readonly ICachedHttpResponseService _cachedHttpClient;
        private readonly Logger _logger;
        private readonly IProvideBookInfo _bookInfo;
        private readonly IAuthorService _authorService;
        private readonly IBookService _bookService;
        private readonly IEditionService _editionService;
        private readonly IHttpRequestBuilderFactory _searchBuilder;
        private readonly ICached<HashSet<string>> _cache;

        public GoodreadsSearchProxy(ICachedHttpResponseService cachedHttpClient,
            IProvideBookInfo bookInfo,
            IAuthorService authorService,
            IBookService bookService,
            IEditionService editionService,
            Logger logger,
            ICacheManager cacheManager)
        {
            _cachedHttpClient = cachedHttpClient;
            _bookInfo = bookInfo;
            _authorService = authorService;
            _bookService = bookService;
            _editionService = editionService;
            _cache = cacheManager.GetCache<HashSet<string>>(GetType());
            _logger = logger;

            _searchBuilder = new HttpRequestBuilder("https://www.goodreads.com/book/auto_complete")
                .AddQueryParam("format", "json")
                .SetHeader("User-Agent",
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/83.0.4103.116 Safari/537.36")
                .KeepAlive()
                .CreateFactory();
        }

        public List<Author> SearchForNewAuthor(string title)
        {
            var books = SearchForNewBook(title, null);

            return books.Select(x => x.Author.Value).ToList();
        }

        public List<Book> SearchForNewBook(string title, string author)
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
                if (author != null)
                {
                    q += " " + author;
                }

                return SearchByField("all", q);
            }
            catch (HttpException)
            {
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
            return SearchByField("isbn", isbn, e => e.Isbn13 = isbn);
        }

        public List<Book> SearchByAsin(string asin)
        {
            return SearchByField("asin", asin, e => e.Asin = asin);
        }

        public List<Book> SearchByGoodreadsId(int id)
        {
            try
            {
                var remote = _bookInfo.GetBookInfo(id.ToString());

                var book = _bookService.FindById(remote.Item2.ForeignBookId);
                var result = book ?? remote.Item2;

                // at this point, book could have the wrong edition.
                // Check if we already have the correct edition.
                var remoteEdition = remote.Item2.Editions.Value.Single(x => x.Monitored);
                var localEdition = _editionService.GetEditionByForeignEditionId(remoteEdition.ForeignEditionId);
                if (localEdition != null)
                {
                    result.Editions = new List<Edition> { localEdition };
                }

                // If we don't have the correct edition in the response, add it in.
                if (!result.Editions.Value.Any(x => x.ForeignEditionId == remoteEdition.ForeignEditionId))
                {
                    result.Editions.Value.ForEach(x => x.Monitored = false);
                    result.Editions.Value.Add(remoteEdition);
                }

                var author = _authorService.FindById(remote.Item1);
                if (author == null)
                {
                    author = new Author
                    {
                        CleanName = Parser.Parser.CleanAuthorName(remote.Item2.AuthorMetadata.Value.Name),
                        Metadata = remote.Item2.AuthorMetadata.Value
                    };
                }

                result.Author = author;

                return new List<Book> { result };
            }
            catch (BookNotFoundException)
            {
                return new List<Book>();
            }
        }

        public List<Book> SearchByField(string field, string query, Action<Edition> applyData = null)
        {
            try
            {
                var httpRequest = _searchBuilder.Create()
                    .AddQueryParam("q", query)
                    .Build();

                var response = _cachedHttpClient.Get<List<SearchJsonResource>>(httpRequest, true, TimeSpan.FromDays(5));

                return response.Resource.SelectList(x =>
                    MapJsonSearchResult(x, response.Resource.Count == 1 ? applyData : null));
            }
            catch (HttpException)
            {
                throw new GoodreadsException("Search for {0} '{1}' failed. Unable to communicate with Goodreads.", field, query);
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, ex.Message);
                throw new GoodreadsException("Search for {0} '{1}' failed. Invalid response received from Goodreads.", field, query);
            }
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

        private Book MapJsonSearchResult(SearchJsonResource resource, Action<Edition> applyData = null)
        {
            var book = _bookService.FindById(resource.WorkId.ToString());
            var edition = _editionService.GetEditionByForeignEditionId(resource.BookId.ToString());

            if (edition == null)
            {
                edition = new Edition
                {
                    ForeignEditionId = resource.BookId.ToString(),
                    Title = resource.BookTitleBare,
                    TitleSlug = resource.BookId.ToString(),
                    Ratings = new Ratings { Votes = resource.RatingsCount, Value = resource.AverageRating },
                    PageCount = resource.PageCount,
                    Overview = resource.Description?.Html ?? string.Empty
                };

                if (applyData != null)
                {
                    applyData(edition);
                }
            }

            edition.Monitored = true;
            edition.ManualAdd = true;

            if (resource.ImageUrl.IsNotNullOrWhiteSpace() && !NoPhotoRegex.IsMatch(resource.ImageUrl))
            {
                edition.Images.Add(new MediaCover.MediaCover
                {
                    Url = FullSizeImageRegex.Replace(resource.ImageUrl),
                    CoverType = MediaCoverTypes.Cover
                });
            }

            if (book == null)
            {
                book = new Book
                {
                    ForeignBookId = resource.WorkId.ToString(),
                    Title = resource.BookTitleBare,
                    TitleSlug = resource.WorkId.ToString(),
                    Ratings = new Ratings { Votes = resource.RatingsCount, Value = resource.AverageRating },
                    AnyEditionOk = true
                };
            }

            if (book.Editions != null)
            {
                if (book.Editions.Value.Any())
                {
                    edition.Monitored = false;
                }

                book.Editions.Value.Add(edition);
            }
            else
            {
                book.Editions = new List<Edition> { edition };
            }

            var authorId = resource.Author.Id.ToString();
            var author = _authorService.FindById(authorId);

            if (author == null)
            {
                author = new Author
                {
                    CleanName = Parser.Parser.CleanAuthorName(resource.Author.Name),
                    Metadata = new AuthorMetadata()
                    {
                        ForeignAuthorId = resource.Author.Id.ToString(),
                        Name = DuplicateSpacesRegex.Replace(resource.Author.Name, " "),
                        TitleSlug = resource.Author.Id.ToString()
                    }
                };
            }

            book.Author = author;
            book.AuthorMetadata = book.Author.Value.Metadata.Value;
            book.AuthorMetadataId = author.AuthorMetadataId;
            book.CleanTitle = book.Title.CleanAuthorName();
            book.SeriesLinks = GoodreadsProxy.MapSearchSeries(resource.Title, resource.BookTitleBare);

            return book;
        }
    }
}
