using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MetadataSource.Goodreads;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.ImportLists.Goodreads
{
    public class GoodreadsBookshelf : GoodreadsImportListBase<GoodreadsBookshelfSettings>
    {
        public GoodreadsBookshelf(IImportListStatusService importListStatusService,
                                  IConfigService configService,
                                  IParsingService parsingService,
                                  IHttpClient httpClient,
                                  Logger logger)
        : base(importListStatusService, configService, parsingService, httpClient, logger)
        {
        }

        public override string Name => "Goodreads Bookshelves";

        public override IList<ImportListItemInfo> Fetch()
        {
            return CleanupListItems(Settings.PlaylistIds.SelectMany(x => Fetch(x)).ToList());
        }

        public IList<ImportListItemInfo> Fetch(string shelf)
        {
            var reviews = new List<ReviewResource>();
            var page = 0;

            while (true)
            {
                var curr = GetReviews(shelf, ++page);

                if (curr == null || curr.Count == 0)
                {
                    break;
                }

                reviews.AddRange(curr);
            }

            return reviews.Select(x => new ImportListItemInfo
            {
                Artist = x.Book.Authors.First().Name.CleanSpaces(),
                Album = x.Book.TitleWithoutSeries.CleanSpaces(),
                AlbumMusicBrainzId = x.Book.Uri.Replace("kca://book/", string.Empty)
            }).ToList();
        }

        public override object RequestAction(string action, IDictionary<string, string> query)
        {
            if (action == "getPlaylists")
            {
                if (Settings.AccessToken.IsNullOrWhiteSpace())
                {
                    return new
                    {
                        playlists = new List<object>()
                    };
                }

                Settings.Validate().Filter("AccessToken").ThrowOnError();

                var shelves = new List<UserShelfResource>();
                var page = 0;

                while (true)
                {
                    var curr = GetShelfList(++page);
                    if (curr == null || curr.Count == 0)
                    {
                        break;
                    }

                    shelves.AddRange(curr);
                }

                return new
                {
                    options = new
                    {
                        user = Settings.UserName,
                        playlists = shelves.OrderBy(p => p.Name)
                        .Select(p => new
                        {
                            id = p.Name,
                            name = p.Name
                        })
                    }
                };
            }
            else
            {
                return base.RequestAction(action, query);
            }
        }

        private IReadOnlyList<UserShelfResource> GetShelfList(int page)
        {
            try
            {
                var builder = RequestBuilder()
                    .SetSegment("route", $"shelf/list.xml")
                    .AddQueryParam("user_id", Settings.UserId)
                    .AddQueryParam("page", page);

                var httpResponse = OAuthGet(builder);

                return httpResponse.Deserialize<PaginatedList<UserShelfResource>>("shelves").List;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Error fetching bookshelves from Goodreads");
                return new List<UserShelfResource>();
            }
        }

        private IReadOnlyList<ReviewResource> GetReviews(string shelf, int page)
        {
            try
            {
                var builder = RequestBuilder()
                    .SetSegment("route", $"review/list.xml")
                    .AddQueryParam("v", 2)
                    .AddQueryParam("id", Settings.UserId)
                    .AddQueryParam("shelf", shelf)
                    .AddQueryParam("per_page", 200)
                    .AddQueryParam("page", page);

                var httpResponse = OAuthGet(builder);

                return httpResponse.Deserialize<PaginatedList<ReviewResource>>("reviews").List;
            }
            catch (Exception ex)
            {
                _logger.Warn(ex, "Error fetching bookshelves from Goodreads");
                return new List<ReviewResource>();
            }
        }
    }
}
