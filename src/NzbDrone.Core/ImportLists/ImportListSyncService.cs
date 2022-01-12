using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Books;
using NzbDrone.Core.Books.Commands;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.ImportLists.Exclusions;
using NzbDrone.Core.IndexerSearch;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.MetadataSource.Goodreads;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.ImportLists
{
    public class ImportListSyncService : IExecute<ImportListSyncCommand>
    {
        private readonly IImportListFactory _importListFactory;
        private readonly IImportListExclusionService _importListExclusionService;
        private readonly IFetchAndParseImportList _listFetcherAndParser;
        private readonly IGoodreadsProxy _goodreadsProxy;
        private readonly IGoodreadsSearchProxy _goodreadsSearchProxy;
        private readonly IAuthorService _authorService;
        private readonly IBookService _bookService;
        private readonly IEditionService _editionService;
        private readonly IAddAuthorService _addAuthorService;
        private readonly IAddBookService _addBookService;
        private readonly IEventAggregator _eventAggregator;
        private readonly IManageCommandQueue _commandQueueManager;
        private readonly Logger _logger;

        public ImportListSyncService(IImportListFactory importListFactory,
                                     IImportListExclusionService importListExclusionService,
                                     IFetchAndParseImportList listFetcherAndParser,
                                     IGoodreadsProxy goodreadsProxy,
                                     IGoodreadsSearchProxy goodreadsSearchProxy,
                                     IAuthorService authorService,
                                     IBookService bookService,
                                     IEditionService editionService,
                                     IAddAuthorService addAuthorService,
                                     IAddBookService addBookService,
                                     IEventAggregator eventAggregator,
                                     IManageCommandQueue commandQueueManager,
                                     Logger logger)
        {
            _importListFactory = importListFactory;
            _importListExclusionService = importListExclusionService;
            _listFetcherAndParser = listFetcherAndParser;
            _goodreadsProxy = goodreadsProxy;
            _goodreadsSearchProxy = goodreadsSearchProxy;
            _authorService = authorService;
            _bookService = bookService;
            _editionService = editionService;
            _addAuthorService = addAuthorService;
            _addBookService = addBookService;
            _eventAggregator = eventAggregator;
            _commandQueueManager = commandQueueManager;
            _logger = logger;
        }

        private List<Book> SyncAll()
        {
            _logger.ProgressInfo("Starting Import List Sync");

            var rssReleases = _listFetcherAndParser.Fetch();

            var reports = rssReleases.ToList();

            return ProcessReports(reports);
        }

        private List<Book> SyncList(ImportListDefinition definition)
        {
            _logger.ProgressInfo(string.Format("Starting Import List Refresh for List {0}", definition.Name));

            var rssReleases = _listFetcherAndParser.FetchSingleList(definition);

            var reports = rssReleases.ToList();

            return ProcessReports(reports);
        }

        private List<Book> ProcessReports(List<ImportListItemInfo> reports)
        {
            var processed = new List<Book>();
            var authorsToAdd = new List<Author>();
            var booksToAdd = new List<Book>();

            _logger.ProgressInfo("Processing {0} list items", reports.Count);

            var reportNumber = 1;

            var listExclusions = _importListExclusionService.All();

            foreach (var report in reports)
            {
                _logger.ProgressTrace("Processing list item {0}/{1}", reportNumber, reports.Count);

                reportNumber++;

                var importList = _importListFactory.Get(report.ImportListId);

                if (report.Book.IsNotNullOrWhiteSpace() || report.EditionGoodreadsId.IsNotNullOrWhiteSpace())
                {
                    if (report.EditionGoodreadsId.IsNullOrWhiteSpace() || report.AuthorGoodreadsId.IsNullOrWhiteSpace() || report.BookGoodreadsId.IsNullOrWhiteSpace())
                    {
                        MapBookReport(report);
                    }

                    ProcessBookReport(importList, report, listExclusions, booksToAdd, authorsToAdd);
                }
                else if (report.Author.IsNotNullOrWhiteSpace() || report.AuthorGoodreadsId.IsNotNullOrWhiteSpace())
                {
                    if (report.AuthorGoodreadsId.IsNullOrWhiteSpace())
                    {
                        MapAuthorReport(report);
                    }

                    ProcessAuthorReport(importList, report, listExclusions, authorsToAdd);
                }
            }

            var addedAuthors = _addAuthorService.AddAuthors(authorsToAdd, false);
            var addedBooks = _addBookService.AddBooks(booksToAdd, false);

            var message = string.Format($"Import List Sync Completed. Items found: {reports.Count}, Authors added: {authorsToAdd.Count}, Books added: {booksToAdd.Count}");

            _logger.ProgressInfo(message);

            var toRefresh = addedAuthors.Select(x => x.Id).Concat(addedBooks.Select(x => x.Author.Value.Id)).Distinct().ToList();
            if (toRefresh.Any())
            {
                _commandQueueManager.Push(new BulkRefreshAuthorCommand(toRefresh, true));
            }

            return processed;
        }

        private void MapBookReport(ImportListItemInfo report)
        {
            if (report.EditionGoodreadsId.IsNotNullOrWhiteSpace() && int.TryParse(report.EditionGoodreadsId, out var goodreadsId))
            {
                // check the local DB
                var edition = _editionService.GetEditionByForeignEditionId(report.EditionGoodreadsId);

                if (edition != null)
                {
                    var book = edition.Book.Value;
                    report.BookGoodreadsId = book.ForeignBookId;
                    report.Book = edition.Title;
                    report.Author ??= book.AuthorMetadata.Value.Name;
                    report.AuthorGoodreadsId ??= book.AuthorMetadata.Value.ForeignAuthorId;
                    return;
                }

                try
                {
                    var remoteBook = _goodreadsProxy.GetBookInfo(report.EditionGoodreadsId);

                    _logger.Trace($"Mapped {report.EditionGoodreadsId} to [{remoteBook.ForeignBookId}] {remoteBook.Title}");

                    report.BookGoodreadsId = remoteBook.ForeignBookId;
                    report.Book = remoteBook.Title;
                    report.Author ??= remoteBook.AuthorMetadata.Value.Name;
                    report.AuthorGoodreadsId ??= remoteBook.AuthorMetadata.Value.Name;
                }
                catch (BookNotFoundException)
                {
                    _logger.Debug($"Nothing found for edition [{report.EditionGoodreadsId}]");
                    report.EditionGoodreadsId = null;
                }
            }
            else
            {
                var mappedBook = _goodreadsSearchProxy.Search($"{report.Book} {report.Author}").FirstOrDefault();

                if (mappedBook == null)
                {
                    _logger.Trace($"Nothing found for {report.Author} - {report.Book}");
                    return;
                }

                _logger.Trace($"Mapped {report.EditionGoodreadsId} to [{mappedBook.WorkId}] {mappedBook.BookTitleBare}");

                report.BookGoodreadsId = mappedBook.WorkId.ToString();
                report.Book = mappedBook.BookTitleBare;
                report.Author ??= mappedBook.Author.Name;
                report.AuthorGoodreadsId ??= mappedBook.Author.Id.ToString();
            }
        }

        private void ProcessBookReport(ImportListDefinition importList, ImportListItemInfo report, List<ImportListExclusion> listExclusions, List<Book> booksToAdd, List<Author> authorsToAdd)
        {
            if (report.EditionGoodreadsId == null)
            {
                return;
            }

            // Check to see if book in DB
            var existingBook = _bookService.FindById(report.BookGoodreadsId);

            // Check to see if book excluded
            var excludedBook = listExclusions.SingleOrDefault(s => s.ForeignId == report.BookGoodreadsId);

            // Check to see if author excluded
            var excludedAuthor = listExclusions.SingleOrDefault(s => s.ForeignId == report.AuthorGoodreadsId);

            if (excludedBook != null)
            {
                _logger.Debug("{0} [{1}] Rejected due to list exclusion", report.EditionGoodreadsId, report.Book);
                return;
            }

            if (excludedAuthor != null)
            {
                _logger.Debug("{0} [{1}] Rejected due to list exclusion for parent author", report.EditionGoodreadsId, report.Book);
                return;
            }

            if (existingBook != null)
            {
                _logger.Debug("{0} [{1}] Rejected, Book Exists in DB.  Ensuring Book and Author monitored.", report.EditionGoodreadsId, report.Book);

                if (importList.ShouldMonitorExisting && importList.ShouldMonitor != ImportListMonitorType.None)
                {
                    if (!existingBook.Monitored)
                    {
                        _bookService.SetBookMonitored(existingBook.Id, true);

                        if (importList.ShouldMonitor == ImportListMonitorType.SpecificBook)
                        {
                            _commandQueueManager.Push(new BookSearchCommand(new List<int> { existingBook.Id }));
                        }
                    }

                    var existingAuthor = existingBook.Author.Value;
                    var doSearch = false;

                    if (importList.ShouldMonitor == ImportListMonitorType.EntireAuthor)
                    {
                        if (existingAuthor.Books.Value.Any(x => !x.Monitored))
                        {
                            doSearch = true;
                            _bookService.SetMonitored(existingAuthor.Books.Value.Select(x => x.Id), true);
                        }
                    }

                    if (!existingAuthor.Monitored)
                    {
                        doSearch = true;
                        existingAuthor.Monitored = true;
                        _authorService.UpdateAuthor(existingAuthor);
                    }

                    if (doSearch)
                    {
                        _commandQueueManager.Push(new MissingBookSearchCommand(existingAuthor.Id));
                    }
                }

                return;
            }

            // Append Book if not already in DB or already on add list
            if (booksToAdd.All(s => s.ForeignBookId != report.BookGoodreadsId))
            {
                var monitored = importList.ShouldMonitor != ImportListMonitorType.None;

                var toAddAuthor = new Author
                {
                    Monitored = monitored,
                    MonitorNewItems = importList.MonitorNewItems,
                    RootFolderPath = importList.RootFolderPath,
                    QualityProfileId = importList.ProfileId,
                    MetadataProfileId = importList.MetadataProfileId,
                    Tags = importList.Tags,
                    AddOptions = new AddAuthorOptions
                    {
                        SearchForMissingBooks = importList.ShouldSearch,
                        Monitored = monitored,
                        Monitor = monitored ? MonitorTypes.All : MonitorTypes.None
                    }
                };

                if (report.AuthorGoodreadsId != null && report.Author != null)
                {
                    toAddAuthor = ProcessAuthorReport(importList, report, listExclusions, authorsToAdd);
                }

                var toAdd = new Book
                {
                    ForeignBookId = report.BookGoodreadsId,
                    Monitored = monitored,
                    AnyEditionOk = true,
                    Editions = new List<Edition>
                    {
                        new Edition
                        {
                            ForeignEditionId = report.EditionGoodreadsId,
                            Monitored = true
                        }
                    },
                    Author = toAddAuthor,
                    AddOptions = new AddBookOptions
                    {
                        // Only search for new book for existing authors
                        // New author searches are triggered by SearchForMissingBooks
                        SearchForNewBook = importList.ShouldSearch && toAddAuthor.Id > 0
                    }
                };

                if (importList.ShouldMonitor == ImportListMonitorType.SpecificBook && toAddAuthor.AddOptions != null)
                {
                    Debug.Assert(toAddAuthor.Id == 0, "new author added but ID is not 0");
                    toAddAuthor.AddOptions.BooksToMonitor.Add(toAdd.ForeignBookId);
                }

                booksToAdd.Add(toAdd);
            }
        }

        private void MapAuthorReport(ImportListItemInfo report)
        {
            var mappedBook = _goodreadsSearchProxy.Search(report.Author).FirstOrDefault();

            if (mappedBook == null)
            {
                _logger.Trace($"Nothing found for {report.Author}");
                return;
            }

            _logger.Trace($"Mapped {report.Author} to [{mappedBook.Author.Name}]");

            report.Author = mappedBook.Author.Name;
            report.AuthorGoodreadsId = mappedBook.Author.Id.ToString();
        }

        private Author ProcessAuthorReport(ImportListDefinition importList, ImportListItemInfo report, List<ImportListExclusion> listExclusions, List<Author> authorsToAdd)
        {
            if (report.AuthorGoodreadsId == null)
            {
                return null;
            }

            // Check to see if author in DB
            var existingAuthor = _authorService.FindById(report.AuthorGoodreadsId);

            // Check to see if author excluded
            var excludedAuthor = listExclusions.Where(s => s.ForeignId == report.AuthorGoodreadsId).SingleOrDefault();

            // Check to see if author in import
            var existingImportAuthor = authorsToAdd.Find(i => i.ForeignAuthorId == report.AuthorGoodreadsId);

            if (excludedAuthor != null)
            {
                _logger.Debug("{0} [{1}] Rejected due to list exclusion", report.AuthorGoodreadsId, report.Author);
                return null;
            }

            if (existingAuthor != null)
            {
                _logger.Debug("{0} [{1}] Rejected, Author Exists in DB.  Ensuring Author monitored", report.AuthorGoodreadsId, report.Author);

                if (importList.ShouldMonitorExisting && !existingAuthor.Monitored)
                {
                    existingAuthor.Monitored = true;
                    _authorService.UpdateAuthor(existingAuthor);
                }

                return existingAuthor;
            }

            if (existingImportAuthor != null)
            {
                _logger.Debug("{0} [{1}] Rejected, Author Exists in Import.", report.AuthorGoodreadsId, report.Author);

                return existingImportAuthor;
            }

            var monitored = importList.ShouldMonitor != ImportListMonitorType.None;

            var toAdd = new Author
            {
                Metadata = new AuthorMetadata
                {
                    ForeignAuthorId = report.AuthorGoodreadsId,
                    Name = report.Author
                },
                Monitored = monitored,
                MonitorNewItems = importList.MonitorNewItems,
                RootFolderPath = importList.RootFolderPath,
                QualityProfileId = importList.ProfileId,
                MetadataProfileId = importList.MetadataProfileId,
                Tags = importList.Tags,
                AddOptions = new AddAuthorOptions
                {
                    SearchForMissingBooks = importList.ShouldSearch,
                    Monitored = monitored,
                    Monitor = monitored ? MonitorTypes.All : MonitorTypes.None
                }
            };

            authorsToAdd.Add(toAdd);

            return toAdd;
        }

        public void Execute(ImportListSyncCommand message)
        {
            List<Book> processed;

            if (message.DefinitionId.HasValue)
            {
                processed = SyncList(_importListFactory.Get(message.DefinitionId.Value));
            }
            else
            {
                processed = SyncAll();
            }

            _eventAggregator.PublishEvent(new ImportListSyncCompleteEvent(processed));
        }
    }
}
