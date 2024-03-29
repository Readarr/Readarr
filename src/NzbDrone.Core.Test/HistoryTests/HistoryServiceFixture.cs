using System.Collections.Generic;
using System.IO;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.Download;
using NzbDrone.Core.History;
using NzbDrone.Core.Indexers;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Core.Test.Qualities;

namespace NzbDrone.Core.Test.HistoryTests
{
    public class HistoryServiceFixture : CoreTest<HistoryService>
    {
        private QualityProfile _profile;
        private QualityProfile _profileCustom;

        [SetUp]
        public void Setup()
        {
            _profile = new QualityProfile
            {
                Cutoff = Quality.MP3.Id,
                Items = QualityFixture.GetDefaultQualities(),
            };

            _profileCustom = new QualityProfile
            {
                Cutoff = Quality.MP3.Id,
                Items = QualityFixture.GetDefaultQualities(Quality.MP3),
            };
        }

        [Test]
        public void should_use_file_name_for_source_title_if_scene_name_is_null()
        {
            var author = Builder<Author>.CreateNew().Build();
            var trackFile = Builder<BookFile>.CreateNew()
                .With(f => f.SceneName = null)
                .With(f => f.Author = author)
                .Build();

            var localTrack = new LocalBook
            {
                Author = author,
                Book = new Book(),
                Path = @"C:\Test\Unsorted\Author.01.Hymn.mp3"
            };

            var downloadClientItem = new DownloadClientItem
            {
                DownloadClientInfo = new DownloadClientItemClientInfo
                {
                    Protocol = DownloadProtocol.Usenet,
                    Id = 1,
                    Name = "sab"
                },
                DownloadId = "abcd"
            };

            Subject.Handle(new TrackImportedEvent(localTrack, trackFile, new List<BookFile>(), true, downloadClientItem));

            Mocker.GetMock<IHistoryRepository>()
                .Verify(v => v.Insert(It.Is<EntityHistory>(h => h.SourceTitle == Path.GetFileNameWithoutExtension(localTrack.Path))));
        }
    }
}
