using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Books;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Download;
using NzbDrone.Core.History;
using Readarr.Api.V1.Author;
using Readarr.Api.V1.Books;
using Readarr.Http;
using Readarr.Http.Extensions;

namespace Readarr.Api.V1.History
{
    [V1ApiController]
    public class HistoryController : Controller
    {
        private readonly IHistoryService _historyService;
        private readonly ICustomFormatCalculationService _formatCalculator;
        private readonly IUpgradableSpecification _upgradableSpecification;
        private readonly IFailedDownloadService _failedDownloadService;
        private readonly IAuthorService _authorService;

        public HistoryController(IHistoryService historyService,
                             ICustomFormatCalculationService formatCalculator,
                             IUpgradableSpecification upgradableSpecification,
                             IFailedDownloadService failedDownloadService,
                             IAuthorService authorService)
        {
            _historyService = historyService;
            _formatCalculator = formatCalculator;
            _upgradableSpecification = upgradableSpecification;
            _failedDownloadService = failedDownloadService;
            _authorService = authorService;
        }

        protected HistoryResource MapToResource(EntityHistory model, bool includeAuthor, bool includeBook)
        {
            var resource = model.ToResource(_formatCalculator);

            if (includeAuthor)
            {
                resource.Author = model.Author.ToResource();
            }

            if (includeBook)
            {
                resource.Book = model.Book.ToResource();
            }

            if (model.Author != null)
            {
                resource.QualityCutoffNotMet = _upgradableSpecification.QualityCutoffNotMet(model.Author.QualityProfile.Value, model.Quality);
            }

            return resource;
        }

        [HttpGet]
        public PagingResource<HistoryResource> GetHistory(bool includeAuthor = false, bool includeBook = false)
        {
            var pagingResource = Request.ReadPagingResourceFromRequest<HistoryResource>();
            var pagingSpec = pagingResource.MapToPagingSpec<HistoryResource, EntityHistory>("date", SortDirection.Descending);

            var eventTypeFilter = pagingResource.Filters.FirstOrDefault(f => f.Key == "eventType");
            var bookIdFilter = pagingResource.Filters.FirstOrDefault(f => f.Key == "bookId");
            var downloadIdFilter = pagingResource.Filters.FirstOrDefault(f => f.Key == "downloadId");

            if (eventTypeFilter != null)
            {
                var filterValue = (EntityHistoryEventType)Convert.ToInt32(eventTypeFilter.Value);
                pagingSpec.FilterExpressions.Add(v => v.EventType == filterValue);
            }

            if (bookIdFilter != null)
            {
                var bookId = Convert.ToInt32(bookIdFilter.Value);
                pagingSpec.FilterExpressions.Add(h => h.BookId == bookId);
            }

            if (downloadIdFilter != null)
            {
                var downloadId = downloadIdFilter.Value;
                pagingSpec.FilterExpressions.Add(h => h.DownloadId == downloadId);
            }

            return pagingSpec.ApplyToPage(_historyService.Paged, h => MapToResource(h, includeAuthor, includeBook));
        }

        [HttpGet("since")]
        public List<HistoryResource> GetHistorySince(DateTime date, EntityHistoryEventType? eventType = null, bool includeAuthor = false, bool includeBook = false)
        {
            return _historyService.Since(date, eventType).Select(h => MapToResource(h, includeAuthor, includeBook)).ToList();
        }

        [HttpGet("author")]
        public List<HistoryResource> GetAuthorHistory(int authorId, int? bookId = null, EntityHistoryEventType? eventType = null, bool includeAuthor = false, bool includeBook = false)
        {
            var author = _authorService.GetAuthor(authorId);

            if (bookId.HasValue)
            {
                return _historyService.GetByBook(bookId.Value, eventType).Select(h =>
                {
                    h.Author = author;

                    return MapToResource(h, includeAuthor, includeBook);
                }).ToList();
            }

            return _historyService.GetByAuthor(authorId, eventType).Select(h =>
            {
                h.Author = author;

                return MapToResource(h, includeAuthor, includeBook);
            }).ToList();
        }

        [HttpPost("failed/{id}")]
        public object MarkAsFailed([FromRoute] int id)
        {
            _failedDownloadService.MarkAsFailed(id);
            return new { };
        }
    }
}
