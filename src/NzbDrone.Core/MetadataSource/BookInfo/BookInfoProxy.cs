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
using NzbDrone.Core.Books;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MediaCover;

namespace NzbDrone.Core.MetadataSource.BookInfo
{
    public class BookInfoProxy : IProvideAuthorInfo
    {
        private readonly IHttpClient _httpClient;
        private readonly Logger _logger;
        private readonly IMetadataRequestBuilder _requestBuilder;
        private readonly ICached<HashSet<string>> _cache;

        public BookInfoProxy(IHttpClient httpClient,
                            IMetadataRequestBuilder requestBuilder,
                            Logger logger,
                            ICacheManager cacheManager)
        {
            _httpClient = httpClient;
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

        public Tuple<string, Book, List<AuthorMetadata>> GetBookInfo(string foreignBookId)
        {
            return null;
        }

        private Author MapAuthor(AuthorResource resource)
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

            var books = resource.Works
                .Where(x => x.ForeignId > 0 && GetAuthorId(x) == resource.ForeignId)
                .Select(MapBook)
                .ToList();

            books.ForEach(x => x.AuthorMetadata = metadata);

            var series = resource.Series.Select(MapSeries).ToList();

            MapSeriesLinks(series, books, resource);

            var result = new Author
            {
                Metadata = metadata,
                CleanName = Parser.Parser.CleanAuthorName(metadata.Name),
                Books = books,
                Series = series
            };

            return result;
        }

        private static void MapSeriesLinks(List<Series> series, List<Book> books, AuthorResource resource)
        {
            var bookDict = books.ToDictionary(x => x.ForeignBookId);
            var seriesDict = series.ToDictionary(x => x.ForeignSeriesId);

            // only take series where there are some works
            foreach (var s in resource.Series.Where(x => x.LinkItems.Any()))
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

                // monitor the most rated release
                var mostPopular = book.Editions.Value.OrderByDescending(x => x.Ratings.Votes).FirstOrDefault();
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

            // sometimes the work release date is after the earliest good edition release
            var editionReleases = book.Editions.Value
                .Where(x => x.ReleaseDate.HasValue && x.ReleaseDate.Value.Month != 1 && x.ReleaseDate.Value.Day != 1)
                .ToList();

            if (editionReleases.Any())
            {
                var earliestRelease = editionReleases.Min(x => x.ReleaseDate.Value);
                if (earliestRelease < book.ReleaseDate)
                {
                    book.ReleaseDate = earliestRelease;
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

        private int GetAuthorId(WorkResource b)
        {
            return b.Books.OrderByDescending(x => x.RatingCount * x.AverageRating).First().Contributors.FirstOrDefault()?.ForeignId ?? 0;
        }
    }
}
