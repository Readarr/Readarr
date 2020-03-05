using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.ImportLists.Exclusions;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.Music;
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
                Artist = "Linkin Park"
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

            Mocker.GetMock<IImportListFactory>()
                .Setup(v => v.Get(It.IsAny<int>()))
                .Returns(new ImportListDefinition { ShouldMonitor = ImportListMonitorType.SpecificAlbum });

            Mocker.GetMock<IFetchAndParseImportList>()
                .Setup(v => v.Fetch())
                .Returns(_importListReports);

            Mocker.GetMock<IImportListExclusionService>()
                .Setup(v => v.All())
                .Returns(new List<ImportListExclusion>());
        }

        private void WithAlbum()
        {
            _importListReports.First().Album = "Meteora";
        }

        private void WithArtistId()
        {
            _importListReports.First().ArtistMusicBrainzId = "f59c5520-5f46-4d2c-b2c4-822eabf53419";
        }

        private void WithAlbumId()
        {
            _importListReports.First().AlbumMusicBrainzId = "09474d62-17dd-3a4f-98fb-04c65f38a479";
        }

        private void WithExistingArtist()
        {
            Mocker.GetMock<IArtistService>()
                .Setup(v => v.FindById(_importListReports.First().ArtistMusicBrainzId))
                .Returns(new Author { ForeignAuthorId = _importListReports.First().ArtistMusicBrainzId });
        }

        private void WithExistingAlbum()
        {
            Mocker.GetMock<IAlbumService>()
                .Setup(v => v.FindById(_importListReports.First().AlbumMusicBrainzId))
                .Returns(new Book { ForeignBookId = _importListReports.First().AlbumMusicBrainzId });
        }

        private void WithExcludedArtist()
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

        private void WithExcludedAlbum()
        {
            Mocker.GetMock<IImportListExclusionService>()
                .Setup(v => v.All())
                .Returns(new List<ImportListExclusion>
                {
                    new ImportListExclusion
                    {
                        ForeignId = "09474d62-17dd-3a4f-98fb-04c65f38a479"
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
        public void should_search_if_artist_title_and_no_artist_id()
        {
            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<ISearchForNewAuthor>()
                .Verify(v => v.SearchForNewAuthor(It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void should_not_search_if_artist_title_and_artist_id()
        {
            WithArtistId();
            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<ISearchForNewAuthor>()
                .Verify(v => v.SearchForNewAuthor(It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_search_if_album_title_and_no_album_id()
        {
            WithAlbum();
            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<ISearchForNewBook>()
                .Verify(v => v.SearchForNewBook(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
        }

        [Test]
        public void should_not_search_if_album_title_and_album_id()
        {
            WithArtistId();
            WithAlbumId();
            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<ISearchForNewBook>()
                .Verify(v => v.SearchForNewBook(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_not_search_if_all_info()
        {
            WithArtistId();
            WithAlbum();
            WithAlbumId();
            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<ISearchForNewAuthor>()
                .Verify(v => v.SearchForNewAuthor(It.IsAny<string>()), Times.Never());

            Mocker.GetMock<ISearchForNewBook>()
                .Verify(v => v.SearchForNewBook(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
        }

        [Test]
        public void should_not_add_if_existing_artist()
        {
            WithArtistId();
            WithExistingArtist();

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddArtistService>()
                .Verify(v => v.AddArtists(It.Is<List<Author>>(t => t.Count == 0)));
        }

        [Test]
        public void should_not_add_if_existing_album()
        {
            WithAlbumId();
            WithExistingAlbum();

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddArtistService>()
                .Verify(v => v.AddArtists(It.Is<List<Author>>(t => t.Count == 0)));
        }

        [Test]
        public void should_add_if_existing_artist_but_new_album()
        {
            WithAlbumId();
            WithExistingArtist();

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddAlbumService>()
                .Verify(v => v.AddAlbums(It.Is<List<Book>>(t => t.Count == 1)));
        }

        [TestCase(ImportListMonitorType.None, false)]
        [TestCase(ImportListMonitorType.SpecificAlbum, true)]
        [TestCase(ImportListMonitorType.EntireArtist, true)]
        public void should_add_if_not_existing_artist(ImportListMonitorType monitor, bool expectedArtistMonitored)
        {
            WithArtistId();
            WithMonitorType(monitor);

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddArtistService>()
                .Verify(v => v.AddArtists(It.Is<List<Author>>(t => t.Count == 1 && t.First().Monitored == expectedArtistMonitored)));
        }

        [TestCase(ImportListMonitorType.None, false)]
        [TestCase(ImportListMonitorType.SpecificAlbum, true)]
        [TestCase(ImportListMonitorType.EntireArtist, true)]
        public void should_add_if_not_existing_album(ImportListMonitorType monitor, bool expectedAlbumMonitored)
        {
            WithAlbumId();
            WithMonitorType(monitor);

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddAlbumService>()
                .Verify(v => v.AddAlbums(It.Is<List<Book>>(t => t.Count == 1 && t.First().Monitored == expectedAlbumMonitored)));
        }

        [Test]
        public void should_not_add_artist_if_excluded_artist()
        {
            WithArtistId();
            WithExcludedArtist();

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddArtistService>()
                .Verify(v => v.AddArtists(It.Is<List<Author>>(t => t.Count == 0)));
        }

        [Test]
        public void should_not_add_album_if_excluded_album()
        {
            WithAlbumId();
            WithExcludedAlbum();

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddAlbumService>()
                .Verify(v => v.AddAlbums(It.Is<List<Book>>(t => t.Count == 0)));
        }

        [Test]
        public void should_not_add_album_if_excluded_artist()
        {
            WithAlbumId();
            WithArtistId();
            WithExcludedArtist();

            Subject.Execute(new ImportListSyncCommand());

            Mocker.GetMock<IAddAlbumService>()
                .Verify(v => v.AddAlbums(It.Is<List<Book>>(t => t.Count == 0)));
        }
    }
}
