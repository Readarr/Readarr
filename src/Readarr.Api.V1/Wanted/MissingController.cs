using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.AuthorStats;
using NzbDrone.Core.Books;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.MediaCover;
using NzbDrone.SignalR;
using Readarr.Api.V1.Books;
using Readarr.Http;
using Readarr.Http.Extensions;

namespace Readarr.Api.V1.Wanted
{
    [V1ApiController("wanted/missing")]
    public class MissingController : BookControllerWithSignalR
    {
        public MissingController(IBookService bookService,
                             ISeriesBookLinkService seriesBookLinkService,
                             IAuthorStatisticsService authorStatisticsService,
                             IMapCoversToLocal coverMapper,
                             IUpgradableSpecification upgradableSpecification,
                             IBroadcastSignalRMessage signalRBroadcaster)
        : base(bookService, seriesBookLinkService, authorStatisticsService, coverMapper, upgradableSpecification, signalRBroadcaster)
        {
        }

        [HttpGet]
        public PagingResource<BookResource> GetMissingBooks(bool includeAuthor = false)
        {
            var pagingResource = Request.ReadPagingResourceFromRequest<BookResource>();
            var pagingSpec = new PagingSpec<Book>
            {
                Page = pagingResource.Page,
                PageSize = pagingResource.PageSize,
                SortKey = pagingResource.SortKey,
                SortDirection = pagingResource.SortDirection
            };

            var monitoredFilter = pagingResource.Filters.FirstOrDefault(f => f.Key == "monitored");

            if (monitoredFilter != null && monitoredFilter.Value == "false")
            {
                pagingSpec.FilterExpressions.Add(v => v.Monitored == false);
            }
            else
            {
                pagingSpec.FilterExpressions.Add(v => v.Monitored == true);
            }

            return pagingSpec.ApplyToPage(_bookService.BooksWithoutFiles, v => MapToResource(v, includeAuthor));
        }
    }
}
