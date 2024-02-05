using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NLog;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Books;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Queue;

namespace NzbDrone.Core.IndexerSearch
{
    internal class BookSearchService : IExecute<BookSearchCommand>,
                               IExecute<MissingBookSearchCommand>,
                               IExecute<CutoffUnmetBookSearchCommand>
    {
        private readonly ISearchForReleases _releaseSearchService;
        private readonly IBookService _bookService;
        private readonly IBookCutoffService _bookCutoffService;
        private readonly IQueueService _queueService;
        private readonly IProcessDownloadDecisions _processDownloadDecisions;
        private readonly Logger _logger;

        public BookSearchService(ISearchForReleases releaseSearchService,
            IBookService bookService,
            IBookCutoffService bookCutoffService,
            IQueueService queueService,
            IProcessDownloadDecisions processDownloadDecisions,
            Logger logger)
        {
            _releaseSearchService = releaseSearchService;
            _bookService = bookService;
            _bookCutoffService = bookCutoffService;
            _queueService = queueService;
            _processDownloadDecisions = processDownloadDecisions;
            _logger = logger;
        }

        private async Task SearchForBulkBooks(List<Book> books, bool userInvokedSearch)
        {
            _logger.ProgressInfo("Performing missing search for {0} books", books.Count);
            var downloadedCount = 0;

            foreach (var book in books.OrderBy(a => a.LastSearchTime ?? DateTime.MinValue))
            {
                List<DownloadDecision> decisions;

                try
                {
                    decisions = await _releaseSearchService.BookSearch(book.Id, false, userInvokedSearch, false);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Unable to search for book: [{0}]", book);
                    continue;
                }

                var processed = await _processDownloadDecisions.ProcessDecisions(decisions);

                downloadedCount += processed.Grabbed.Count;
            }

            _logger.ProgressInfo("Completed search for {0} books. {1} reports downloaded.", books.Count, downloadedCount);
        }

        public void Execute(BookSearchCommand message)
        {
            foreach (var bookId in message.BookIds)
            {
                var decisions = _releaseSearchService.BookSearch(bookId, false, message.Trigger == CommandTrigger.Manual, false).GetAwaiter().GetResult();
                var processed = _processDownloadDecisions.ProcessDecisions(decisions).GetAwaiter().GetResult();

                _logger.ProgressInfo("Book search completed. {0} reports downloaded.", processed.Grabbed.Count);
            }
        }

        public void Execute(MissingBookSearchCommand message)
        {
            List<Book> books;

            if (message.AuthorId.HasValue)
            {
                var authorId = message.AuthorId.Value;

                var pagingSpec = new PagingSpec<Book>
                {
                    Page = 1,
                    PageSize = 100000,
                    SortDirection = SortDirection.Ascending,
                    SortKey = "Id"
                };

                pagingSpec.FilterExpressions.Add(v => v.Monitored == true && v.Author.Value.Monitored == true);

                books = _bookService.BooksWithoutFiles(pagingSpec).Records.Where(e => e.AuthorId.Equals(authorId)).ToList();
            }
            else
            {
                var pagingSpec = new PagingSpec<Book>
                {
                    Page = 1,
                    PageSize = 100000,
                    SortDirection = SortDirection.Ascending,
                    SortKey = "Id"
                };

                pagingSpec.FilterExpressions.Add(v => v.Monitored == true && v.Author.Value.Monitored == true);

                books = _bookService.BooksWithoutFiles(pagingSpec).Records.ToList();
            }

            var queue = _queueService.GetQueue().Where(q => q.Book != null).Select(q => q.Book.Id);
            var missing = books.Where(e => !queue.Contains(e.Id)).ToList();

            SearchForBulkBooks(missing, message.Trigger == CommandTrigger.Manual).GetAwaiter().GetResult();
        }

        public void Execute(CutoffUnmetBookSearchCommand message)
        {
            var pagingSpec = new PagingSpec<Book>
            {
                Page = 1,
                PageSize = 100000,
                SortDirection = SortDirection.Ascending,
                SortKey = "Id"
            };

            pagingSpec.FilterExpressions.Add(v => v.Monitored == true && v.Author.Value.Monitored == true);

            var books = _bookCutoffService.BooksWhereCutoffUnmet(pagingSpec).Records.ToList();

            var queue = _queueService.GetQueue().Where(q => q.Book != null).Select(q => q.Book.Id);
            var cutoffUnmet = books.Where(e => !queue.Contains(e.Id)).ToList();

            SearchForBulkBooks(cutoffUnmet, message.Trigger == CommandTrigger.Manual).GetAwaiter().GetResult();
        }
    }
}
