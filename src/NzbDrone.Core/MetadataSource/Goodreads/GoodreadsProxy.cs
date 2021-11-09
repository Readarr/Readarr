using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Books;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.Http;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.Parser;

namespace NzbDrone.Core.MetadataSource.Goodreads
{
    public class GoodreadsProxy : IProvideBookInfo, IProvideSeriesInfo, IProvideListInfo
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
        private readonly IAuthorService _authorService;
        private readonly IEditionService _editionService;
        private readonly IHttpRequestBuilderFactory _requestBuilder;
        private readonly ICached<HashSet<string>> _cache;

        public GoodreadsProxy(ICachedHttpResponseService cachedHttpClient,
                              IAuthorService authorService,
                              IEditionService editionService,
                              Logger logger,
                              ICacheManager cacheManager)
        {
            _cachedHttpClient = cachedHttpClient;
            _authorService = authorService;
            _editionService = editionService;
            _cache = cacheManager.GetCache<HashSet<string>>(GetType());
            _logger = logger;

            _requestBuilder = new HttpRequestBuilder("https://www.goodreads.com/{route}")
                .AddQueryParam("key", new string("gSuM2Onzl6sjMU25HY1Xcd".Reverse().ToArray()))
                .AddQueryParam("_nc", "1")
                .SetHeader("User-Agent", "Dalvik/1.6.0 (Linux; U; Android 4.1.2; GT-I9100 Build/JZO54K)")
                .KeepAlive()
                .CreateFactory();
        }

        public HashSet<string> GetChangedAuthors(DateTime startTime)
        {
            return null;
        }

        public Author GetAuthorInfo(string foreignAuthorId, bool useCache = true)
        {
            _logger.Debug("Getting Author details GoodreadsId of {0}", foreignAuthorId);

            var httpRequest = _requestBuilder.Create()
                .SetSegment("route", $"author/show/{foreignAuthorId}.xml")
                .AddQueryParam("exclude_books", "true")
                .Build();

            httpRequest.AllowAutoRedirect = true;
            httpRequest.SuppressHttpError = true;

            var httpResponse = _cachedHttpClient.Get(httpRequest, useCache, TimeSpan.FromDays(30));

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

            var resource = httpResponse.Deserialize<AuthorResource>();
            var author = new Author
            {
                Metadata = MapAuthor(resource)
            };
            author.CleanName = Parser.Parser.CleanAuthorName(author.Metadata.Value.Name);

            // we can only get a rating from the author list page...
            var listResource = GetAuthorBooksPageResource(foreignAuthorId, 10, 1);
            var authorResource = listResource.List.SelectMany(x => x.Authors).FirstOrDefault(a => a.Id.ToString() == foreignAuthorId);
            author.Metadata.Value.Ratings = new Ratings
            {
                Votes = authorResource?.RatingsCount ?? 0,
                Value = authorResource?.AverageRating ?? 0
            };

            return author;
        }

        public Author GetAuthorAndBooks(string foreignAuthorId, double minPopularity = 0)
        {
            var author = GetAuthorInfo(foreignAuthorId);

            var bookList = GetAuthorBooks(foreignAuthorId, minPopularity);
            var books = bookList.Select((x, i) =>
            {
                _logger.ProgressDebug($"{author}: Fetching book {i}/{bookList.Count}");
                return GetBookInfo(x.Editions.Value.First().ForeignEditionId).Item2;
            }).ToList();

            var existingAuthor = _authorService.FindById(foreignAuthorId);
            if (existingAuthor != null)
            {
                var existingEditions = _editionService.GetEditionsByAuthor(existingAuthor.Id);
                var extraEditionIds = existingEditions
                    .Select(x => x.ForeignEditionId)
                    .Except(books.Select(x => x.Editions.Value.First().ForeignEditionId))
                    .ToList();

                _logger.Debug($"Getting data for extra editions {extraEditionIds.ConcatToString()}");

                var extraEditions = new List<Tuple<string, Book, List<AuthorMetadata>>>();
                foreach (var id in extraEditionIds)
                {
                    if (TryGetBookInfo(id, true, out var result))
                    {
                        extraEditions.Add(result);
                    }
                }

                var bookDict = books.ToDictionary(x => x.ForeignBookId);
                foreach (var edition in extraEditions)
                {
                    var b = edition.Item2;

                    if (bookDict.TryGetValue(b.ForeignBookId, out var book))
                    {
                        book.Editions.Value.Add(b.Editions.Value.First());
                    }
                    else
                    {
                        bookDict.Add(b.ForeignBookId, b);
                    }
                }

                books = bookDict.Values.ToList();
            }

            books.ForEach(x => x.AuthorMetadata = author.Metadata.Value);
            author.Books = books;

            author.Series = GetAuthorSeries(foreignAuthorId, author.Books);

            return author;
        }

        private List<Book> GetAuthorBooks(string foreignAuthorId, double minPopularity)
        {
            var perPage = 100;
            var page = 0;

            var result = new List<Book>();
            List<Book> current;
            IEnumerable<Book> filtered;

            do
            {
                current = GetAuthorBooksPage(foreignAuthorId, perPage, ++page);
                filtered = current.Where(x => x.Editions.Value.First().Ratings.Popularity >= minPopularity);
                result.AddRange(filtered);
            }
            while (current.Count == perPage && filtered.Any());

            return result;
        }

        private List<Book> GetAuthorBooksPage(string foreignAuthorId, int perPage, int page)
        {
            var resource = GetAuthorBooksPageResource(foreignAuthorId, perPage, page);

            var books = resource?.List.Where(x => x.Authors.First().Id.ToString() == foreignAuthorId)
                .Select(MapBook)
                .ToList() ??
                new List<Book>();

            books.ForEach(x => x.CleanTitle = x.Title.CleanAuthorName());

            return books;
        }

        private AuthorBookListResource GetAuthorBooksPageResource(string foreignAuthorId, int perPage, int page)
        {
            _logger.Debug("Getting Author Books with GoodreadsId of {0}", foreignAuthorId);

            var httpRequest = _requestBuilder.Create()
                .SetSegment("route", $"author/list/{foreignAuthorId}.xml")
                .AddQueryParam("per_page", perPage)
                .AddQueryParam("page", page)
                .AddQueryParam("sort", "popularity")
                .Build();

            httpRequest.AllowAutoRedirect = true;
            httpRequest.SuppressHttpError = true;

            var httpResponse = _cachedHttpClient.Get(httpRequest, true, TimeSpan.FromDays(7));

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

            return httpResponse.Deserialize<AuthorBookListResource>();
        }

        private List<Series> GetAuthorSeries(string foreignAuthorId, List<Book> books)
        {
            _logger.Debug("Getting Author Series with GoodreadsId of {0}", foreignAuthorId);

            var httpRequest = _requestBuilder.Create()
                .SetSegment("route", $"series/list/{foreignAuthorId}.xml")
                .Build();

            httpRequest.AllowAutoRedirect = true;
            httpRequest.SuppressHttpError = true;

            var httpResponse = _cachedHttpClient.Get(httpRequest, true, TimeSpan.FromDays(90));

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

            var resource = httpResponse.Deserialize<AuthorSeriesListResource>();

            var result = new List<Series>();
            var bookDict = books.ToDictionary(x => x.ForeignBookId);

            // only take series where there are some works
            // and the title is not null
            // e.g. https://www.goodreads.com/series/work/6470221?format=xml is in series 260494
            // which has a null title and is not shown anywhere on goodreads webpage
            foreach (var seriesResource in resource.List.Where(x => x.Title.IsNotNullOrWhiteSpace() && x.Works.Any()))
            {
                var series = MapSeries(seriesResource);
                series.LinkItems = new List<SeriesBookLink>();

                var works = seriesResource.Works
                    .Where(x => x.BestBook.AuthorId.ToString() == foreignAuthorId &&
                           bookDict.ContainsKey(x.Id.ToString()));
                foreach (var work in works)
                {
                    series.LinkItems.Value.Add(new SeriesBookLink
                    {
                        Book = bookDict[work.Id.ToString()],
                        Series = series,
                        IsPrimary = true,
                        Position = work.UserPosition
                    });
                }

                if (series.LinkItems.Value.Any())
                {
                    result.Add(series);
                }
            }

            return result;
        }

        public SeriesResource GetSeriesInfo(int foreignSeriesId, bool useCache = true)
        {
            _logger.Debug("Getting Series with GoodreadsId of {0}", foreignSeriesId);

            var httpRequest = _requestBuilder.Create()
                .SetSegment("route", $"series/{foreignSeriesId}")
                .AddQueryParam("format", "xml")
                .Build();

            httpRequest.AllowAutoRedirect = true;
            httpRequest.SuppressHttpError = true;

            var httpResponse = _cachedHttpClient.Get(httpRequest, useCache, TimeSpan.FromDays(7));

            if (httpResponse.HasHttpError)
            {
                if (httpResponse.StatusCode == HttpStatusCode.BadRequest)
                {
                    throw new BadRequestException(foreignSeriesId.ToString());
                }
                else
                {
                    throw new HttpException(httpRequest, httpResponse);
                }
            }

            var resource = httpResponse.Deserialize<ShowSeriesResource>();

            return resource.Series;
        }

        public ListResource GetListInfo(int foreignListId, int page, bool useCache = true)
        {
            _logger.Debug("Getting List with GoodreadsId of {0}", foreignListId);

            var httpRequest = new HttpRequestBuilder("https://www.goodreads.com/book/list/listopia.xml")
                .AddQueryParam("key", new string("whFzJP3Ud0gZsAdyXxSr7T".Reverse().ToArray()))
                .AddQueryParam("_nc", "1")
                .AddQueryParam("format", "xml")
                .AddQueryParam("id", foreignListId)
                .AddQueryParam("items_per_page", 30)
                .AddQueryParam("page", page)
                .SetHeader("User-Agent", "Goodreads/3.33.1 (iPhone; iOS 14.3; Scale/3.00)")
                .SetHeader("X_APPLE_DEVICE_MODEL", "iPhone")
                .SetHeader("x-gr-os-version", "iOS 14.3")
                .SetHeader("Accept-Language", "en-GB;q=1")
                .SetHeader("X_APPLE_APP_VERSION", "761")
                .SetHeader("x-gr-app-version", "761")
                .SetHeader("x-gr-hw-model", "iPhone11,6")
                .SetHeader("X_APPLE_SYSTEM_VERSION", "14.3")
                .KeepAlive()
                .Build();

            httpRequest.AllowAutoRedirect = true;
            httpRequest.SuppressHttpError = true;

            var httpResponse = _cachedHttpClient.Get(httpRequest, useCache, TimeSpan.FromDays(7));

            if (httpResponse.HasHttpError)
            {
                if (httpResponse.StatusCode == HttpStatusCode.BadRequest)
                {
                    throw new BadRequestException(foreignListId.ToString());
                }
                else
                {
                    throw new HttpException(httpRequest, httpResponse);
                }
            }

            return httpResponse.Deserialize<ListResource>();
        }

        private bool TryGetBookInfo(string foreignEditionId, bool useCache, out Tuple<string, Book, List<AuthorMetadata>> result)
        {
            try
            {
                result = GetBookInfo(foreignEditionId, useCache);
                return true;
            }
            catch (BookNotFoundException e)
            {
                result = null;
                _logger.Warn(e, "Book not found");
                return false;
            }
        }

        public Tuple<string, Book, List<AuthorMetadata>> GetBookInfo(string foreignEditionId, bool useCache = true)
        {
            _logger.Debug("Getting Book with GoodreadsId of {0}", foreignEditionId);

            var httpRequest = _requestBuilder.Create()
                .SetSegment("route", $"api/book/basic_book_data/{foreignEditionId}")
                .AddQueryParam("format", "xml")
                .Build();

            httpRequest.AllowAutoRedirect = true;
            httpRequest.SuppressHttpError = true;

            var httpResponse = _cachedHttpClient.Get(httpRequest, useCache, TimeSpan.FromDays(90));

            if (httpResponse.HasHttpError)
            {
                if (httpResponse.StatusCode == HttpStatusCode.NotFound)
                {
                    throw new BookNotFoundException(foreignEditionId);
                }
                else if (httpResponse.StatusCode == HttpStatusCode.BadRequest)
                {
                    throw new BadRequestException(foreignEditionId);
                }
                else
                {
                    throw new HttpException(httpRequest, httpResponse);
                }
            }

            var resource = httpResponse.Deserialize<BookResource>();

            var book = MapBook(resource);
            book.CleanTitle = Parser.Parser.CleanAuthorName(book.Title);

            var authors = resource.Authors.SelectList(MapAuthor);
            book.AuthorMetadata = authors.First();

            return new Tuple<string, Book, List<AuthorMetadata>>(resource.Authors.First().Id.ToString(), book, authors);
        }

        private static AuthorMetadata MapAuthor(AuthorResource resource)
        {
            var author = new AuthorMetadata
            {
                ForeignAuthorId = resource.Id.ToString(),
                TitleSlug = resource.Id.ToString(),
                Name = resource.Name.CleanSpaces(),
                Overview = resource.About,
                Gender = resource.Gender,
                Hometown = resource.Hometown,
                Born = resource.BornOnDate,
                Died = resource.DiedOnDate,
                Status = resource.DiedOnDate < DateTime.UtcNow ? AuthorStatusType.Ended : AuthorStatusType.Continuing
            };

            author.SortName = author.Name.ToLower();
            author.NameLastFirst = author.Name.ToLastFirst();
            author.SortNameLastFirst = author.NameLastFirst.ToLower();

            if (!NoPhotoRegex.IsMatch(resource.LargeImageUrl))
            {
                author.Images.Add(new MediaCover.MediaCover
                {
                    Url = FullSizeImageRegex.Replace(resource.LargeImageUrl),
                    CoverType = MediaCoverTypes.Poster
                });
            }

            author.Links.Add(new Links { Url = resource.Link, Name = "Goodreads" });

            return author;
        }

        private static AuthorMetadata MapAuthor(AuthorSummaryResource resource)
        {
            var author = new AuthorMetadata
            {
                ForeignAuthorId = resource.Id.ToString(),
                Name = resource.Name.CleanSpaces(),
                TitleSlug = resource.Id.ToString()
            };

            author.SortName = author.Name.ToLower();
            author.NameLastFirst = author.Name.ToLastFirst();
            author.SortNameLastFirst = author.NameLastFirst.ToLower();

            if (resource.RatingsCount.HasValue)
            {
                author.Ratings = new Ratings
                {
                    Votes = resource.RatingsCount ?? 0,
                    Value = resource.AverageRating ?? 0
                };
            }

            if (!NoPhotoRegex.IsMatch(resource.ImageUrl))
            {
                author.Images.Add(new MediaCover.MediaCover
                {
                    Url = FullSizeImageRegex.Replace(resource.ImageUrl),
                    CoverType = MediaCoverTypes.Poster
                });
            }

            return author;
        }

        private static Series MapSeries(SeriesResource resource)
        {
            var series = new Series
            {
                ForeignSeriesId = resource.Id.ToString(),
                Title = resource.Title,
                Description = resource.Description,
                Numbered = resource.IsNumbered,
                WorkCount = resource.SeriesWorksCount,
                PrimaryWorkCount = resource.PrimaryWorksCount
            };

            return series;
        }

        private static Book MapBook(BookResource resource)
        {
            var book = new Book
            {
                ForeignBookId = resource.Work.Id.ToString(),
                Title = (resource.Work.OriginalTitle ?? resource.TitleWithoutSeries).CleanSpaces(),
                TitleSlug = resource.Work.Id.ToString(),
                ReleaseDate = resource.Work.OriginalPublicationDate ?? resource.PublicationDate,
                Ratings = new Ratings { Votes = resource.Work.RatingsCount, Value = resource.Work.AverageRating },
                AnyEditionOk = true
            };

            if (resource.EditionsUrl != null)
            {
                book.Links.Add(new Links { Url = resource.EditionsUrl, Name = "Goodreads Editions" });
            }

            var edition = new Edition
            {
                ForeignEditionId = resource.Id.ToString(),
                TitleSlug = resource.Id.ToString(),
                Isbn13 = resource.Isbn13,
                Asin = resource.Asin ?? resource.KindleAsin,
                Title = resource.TitleWithoutSeries,
                Language = resource.LanguageCode,
                Overview = resource.Description,
                Format = resource.Format,
                IsEbook = resource.IsEbook,
                Disambiguation = resource.EditionInformation,
                Publisher = resource.Publisher,
                PageCount = resource.Pages,
                ReleaseDate = resource.PublicationDate,
                Ratings = new Ratings { Votes = resource.RatingsCount, Value = resource.AverageRating },
                Monitored = true
            };

            if (resource.ImageUrl.IsNotNullOrWhiteSpace() && !NoPhotoRegex.IsMatch(resource.ImageUrl))
            {
                edition.Images.Add(new MediaCover.MediaCover
                {
                    Url = FullSizeImageRegex.Replace(resource.ImageUrl),
                    CoverType = MediaCoverTypes.Cover
                });
            }

            edition.Links.Add(new Links { Url = resource.Url, Name = "Goodreads Book" });

            book.Editions = new List<Edition> { edition };

            Debug.Assert(!book.Editions.Value.Any() || book.Editions.Value.Count(x => x.Monitored) == 1, "one edition monitored");

            book.SeriesLinks = MapSearchSeries(resource.Title, resource.TitleWithoutSeries);

            return book;
        }

        public static List<SeriesBookLink> MapSearchSeries(string title, string titleWithoutSeries)
        {
            if (title != titleWithoutSeries &&
                title.Substring(0, titleWithoutSeries.Length) == titleWithoutSeries)
            {
                var seriesText = title.Substring(titleWithoutSeries.Length);

                foreach (var regex in SeriesRegex)
                {
                    var match = regex.Match(seriesText);

                    if (match.Success)
                    {
                        var series = match.Groups["series"].Value;
                        var position = match.Groups["position"].Value;

                        return new List<SeriesBookLink>
                        {
                            new SeriesBookLink
                            {
                                Series = new Series
                                {
                                    Title = series
                                },
                                Position = position
                            }
                        };
                    }
                }
            }

            return new List<SeriesBookLink>();
        }
    }
}
