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
    [V1ApiController("wanted/cutoff")]
    public class CutoffController : BookControllerWithSignalR
    {
        private readonly IBookCutoffService _bookCutoffService;

        public CutoffController(IBookCutoffService bookCutoffService,
                            IBookService bookService,
                            ISeriesBookLinkService seriesBookLinkService,
                            IAuthorStatisticsService authorStatisticsService,
                            IMapCoversToLocal coverMapper,
                            IUpgradableSpecification upgradableSpecification,
                            IBroadcastSignalRMessage signalRBroadcaster)
        : base(bookService, seriesBookLinkService, authorStatisticsService, coverMapper, upgradableSpecification, signalRBroadcaster)
        {
            _bookCutoffService = bookCutoffService;
        }

        [HttpGet]
        public PagingResource<BookResource> GetCutoffUnmetBooks(bool includeAuthor = false)
        {
            var pagingResource = Request.ReadPagingResourceFromRequest<BookResource>();
            var pagingSpec = new PagingSpec<Book>
            {
                Page = pagingResource.Page,
                PageSize = pagingResource.PageSize,
                SortKey = pagingResource.SortKey,
                SortDirection = pagingResource.SortDirection
            };

            var filter = pagingResource.Filters.FirstOrDefault(f => f.Key == "monitored");

            if (filter != null && filter.Value == "false")
            {
                pagingSpec.FilterExpressions.Add(v => v.Monitored == false || v.Author.Value.Monitored == false);
            }
            else
            {
                pagingSpec.FilterExpressions.Add(v => v.Monitored == true && v.Author.Value.Monitored == true);
            }

            return pagingSpec.ApplyToPage(_bookCutoffService.BooksWhereCutoffUnmet, v => MapToResource(v, includeAuthor));
        }
    }
}
