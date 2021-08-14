using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.ImportLists.Exclusions;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ImportListTests
{
    public class ImportListSyncServiceFixture : CoreTest<ImportListSyncService>
    {
        private List<ImportListItemInfo> _importListReports;

        [SetUp]
        public void SetUp()
        {
            var importListItem1 = new ImportListItemInfo
            {
                Author = "Linkin Park"
            };

            _importListReports = new List<ImportListItemInfo> { importListItem1 };

            Mocker.GetMock<IFetchAndParseImportList>()
                .Setup(v => v.Fetch())
                .Returns(_importListReports);

            Mocker.GetMock<ISearchForNewAuthor>()
                .Setup(v => v.SearchForNewAuthor(It.IsAny<string>()))
                .Returns(new List<Author>());

            Mocker.GetMock<ISearchForNewBook>()
                .Setup(v => v.SearchForNewBook(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new List<Book>());

            Mocker.GetMock<ISearchForNewBook>()
                .Setup(v => v.SearchByGoodreadsId(It.IsAny<int>()))
                .Returns<int>(x => Builder<Book>
                              .CreateListOfSize(1)
                              .TheFirst(1)
                              .With(b => b.ForeignBookId = "4321")
                              .With(b => b.Editions = Builder<Edition>
                                    .CreateListOfSize(1)
                                    .TheFirst(1)
                                    .With(e => e.ForeignEditionId = x.ToString())
                                    .With(e => e.Monitored = true)
                                    .BuildList())
                              .BuildList());

            Mocker.GetMock<IImportListFactory>()
                .Setup(v => v.Get(It.IsAny<int>()))
                .Returns(new ImportListDefinition { ShouldMonitor = ImportListMonitorType.SpecificBook });

            Mocker.GetMock<IFetchAndParseImportList>()
                .Setup(v => v.Fetch())
                .Returns(_importListReports);

            Mocker.GetMock<IImportListExclusionService>()
                .Setup(v => v.All())
                .Returns(new List<ImportListExclusion>());

            Mocker.GetMock<IAddBookService>()
                .Setup(v => v.AddBooks(It.IsAny<List<Book>>(), false))
                .Returns<List<Book>, bool>((x, y) => x);

            Mocker.GetMock<IAddAuthorService>()
                .Setup(v => v.AddAuthors(It.IsAny<List<Author>>(), false))
                .Returns<List<Author>, bool>((x, y) => x);
        }

        private void WithBook()
        {
            _importListReports.First().Book = "Meteora";
        }

        private void WithAuthorId()
        {
            _importListReports.First().AuthorGoodreadsId = "f59c5520-5f46-4d2c-b2c4-822eabf53419";
        }

        private void WithBookId()
        {
            _importListReports.First().EditionGoodreadsId = "1234";
        }

        private void WithSecondBook()
        {
            var importListItem2 = new ImportListItemInfo
            {
                Author = "Linkin Park",
                AuthorGoodreadsId = "f59c5520-5f46-4d2c-b2c4-822eabf53419",
                Book = "Meteora 2",
                EditionGoodreadsId = "5678",
                BookGoodreadsId = "8765"
            };
            _importListReports.Add(importListItem2);
        }

        private void WithExistingAuthor()
        {
            Mocker.GetMock<IAuthorService>()
                .Setup(v => v.FindById(_importListReports.First().AuthorGoodreadsId))
                .Returns(new Author { ForeignAuthorId = _importListReports.First().AuthorGoodreadsId });
        }

        private void WithExistingBook()
        {
            Mocker.GetMock<IBookService>()
                .Setup(v => v.FindById(_importListReports.First().EditionGoodreadsId))
                .Returns(new Book { ForeignBookId = _importListReports.First().EditionGoodreadsId });
        }

        private void WithExcludedAuthor()
        {
            Mocker.GetMock<IImportListExclusionService>()
                .Setup(v => v.All())
                .Returns(new List<ImportListExclusion>
                {
                    new ImportListExclusion
                    {
                        ForeignId = "f59c5520-5f46-4d2c-b2c4-822eabf53419"
                    }
                });
        }

        private void WithExcludedBook()
        {
            Mocker.GetMock<IImportListExclusionService>()
                .Setup(v => v.All())
                .Returns(new List<ImportListExclusion>
                {
                    new ImportListExclusion
                    {
                        ForeignId = "4321"
                    }
                });
        }

        private void WithMonitorType(ImportListMonitorType monitor)
        {
            Mocker.GetMock<IImportListFactory>()
                .Setup(v => v.Get(It.IsAny<int>()))
                .Returns(new ImportListDefinition { ShouldMonitor = monitor });
        }

        [Test]
        public void should_search_if_author_title_and_no_author_id()
        {
            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<ISearchForNewAuthor>()
                .Verify(v => v.SearchForNewAuthor(It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void should_not_search_if_author_title_and_author_id()
        {
            WithAuthorId();
            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<ISearchForNewAuthor>()
                .Verify(v => v.SearchForNewAuthor(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_search_if_book_title_and_no_book_id()
        {
            WithBook();
            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<ISearchForNewBook>()
                .Verify(v => v.SearchForNewBook(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void should_not_search_if_book_title_and_book_id()
        {
            WithAuthorId();
            WithBookId();
            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<ISearchForNewBook>()
                .Verify(v => v.SearchForNewBook(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_not_search_if_all_info()
        {
            WithAuthorId();
            WithBook();
            WithBookId();
            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<ISearchForNewAuthor>()
                .Verify(v => v.SearchForNewAuthor(It.IsAny<string>()), Times.Never());

            Mocker.GetMock<ISearchForNewBook>()
                .Verify(v => v.SearchForNewBook(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_not_add_if_existing_author()
        {
            WithAuthorId();
            WithExistingAuthor();

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddAuthorService>()
                .Verify(v => v.AddAuthors(It.Is<List<Author>>(t => t.Count == 0), false));
        }

        [Test]
        public void should_not_add_if_existing_book()
        {
            WithBookId();
            WithExistingBook();

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddAuthorService>()
                .Verify(v => v.AddAuthors(It.Is<List<Author>>(t => t.Count == 0), false));
        }

        [Test]
        public void should_add_if_existing_author_but_new_book()
        {
            WithBookId();
            WithExistingAuthor();

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddBookService>()
                .Verify(v => v.AddBooks(It.Is<List<Book>>(t => t.Count == 1), false));
        }

        [TestCase(ImportListMonitorType.None, false)]
        [TestCase(ImportListMonitorType.SpecificBook, true)]
        [TestCase(ImportListMonitorType.EntireAuthor, true)]
        public void should_add_if_not_existing_author(ImportListMonitorType monitor, bool expectedAuthorMonitored)
        {
            WithAuthorId();
            WithMonitorType(monitor);

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddAuthorService>()
                .Verify(v => v.AddAuthors(It.Is<List<Author>>(t => t.Count == 1 && t.First().Monitored == expectedAuthorMonitored), false));
        }

        [TestCase(ImportListMonitorType.None, false)]
        [TestCase(ImportListMonitorType.SpecificBook, true)]
        [TestCase(ImportListMonitorType.EntireAuthor, true)]
        public void should_add_if_not_existing_book(ImportListMonitorType monitor, bool expectedBookMonitored)
        {
            WithBookId();
            WithMonitorType(monitor);

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddBookService>()
                .Verify(v => v.AddBooks(It.Is<List<Book>>(t => t.Count == 1 && t.First().Monitored == expectedBookMonitored), false));
        }

        [Test]
        public void should_not_add_author_if_excluded_author()
        {
            WithAuthorId();
            WithExcludedAuthor();

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddAuthorService>()
                .Verify(v => v.AddAuthors(It.Is<List<Author>>(t => t.Count == 0), false));
        }

        [Test]
        public void should_not_add_book_if_excluded_book()
        {
            WithBookId();
            WithExcludedBook();

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddBookService>()
                .Verify(v => v.AddBooks(It.Is<List<Book>>(t => t.Count == 0), false));
        }

        [Test]
        public void should_not_add_book_if_excluded_author()
        {
            WithBookId();
            WithAuthorId();
            WithExcludedAuthor();

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddBookService>()
                .Verify(v => v.AddBooks(It.Is<List<Book>>(t => t.Count == 0), false));
        }

        [TestCase(ImportListMonitorType.None, 0, false)]
        [TestCase(ImportListMonitorType.SpecificBook, 2, true)]
        [TestCase(ImportListMonitorType.EntireAuthor, 0, true)]
        public void should_add_two_books(ImportListMonitorType monitor, int expectedBooksMonitored, bool expectedAuthorMonitored)
        {
            WithBook();
            WithBookId();
            WithSecondBook();
            WithAuthorId();
            WithMonitorType(monitor);

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddBookService>()
                .Verify(v => v.AddBooks(It.Is<List<Book>>(t => t.Count == 2), false));
            Mocker.GetMock<IAddAuthorService>()
                .Verify(v => v.AddAuthors(It.Is<List<Author>>(t => t.Count == 1 &&
                                                                   t.First().AddOptions.BooksToMonitor.Count == expectedBooksMonitored &&
                                                                   t.First().Monitored == expectedAuthorMonitored), false));
        }
    }
}
