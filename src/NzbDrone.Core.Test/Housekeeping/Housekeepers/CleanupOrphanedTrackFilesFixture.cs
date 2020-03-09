using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Housekeeping.Housekeepers;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Housekeeping.Housekeepers
{
    [TestFixture]
    public class CleanupOrphanedTrackFilesFixture : DbTest<CleanupOrphanedBookFiles, BookFile>
    {
        [Test]
        public void should_unlink_orphaned_track_files()
        {
            var trackFile = Builder<BookFile>.CreateNew()
                .With(h => h.Quality = new QualityModel())
                .With(h => h.BookId = 1)
                .BuildNew();

            Db.Insert(trackFile);
            Subject.Clean();
            AllStoredModels[0].BookId.Should().Be(0);
        }

        [Test]
        public void should_not_unlink_unorphaned_track_files()
        {
            var trackFiles = Builder<BookFile>.CreateListOfSize(2)
                .All()
                .With(h => h.Quality = new QualityModel())
                .With(h => h.BookId = 1)
                .BuildListOfNew();

            Db.InsertMany(trackFiles);

            var track = Builder<Book>.CreateNew()
                                          .With(e => e.BookFileId = trackFiles.First().Id)
                                          .BuildNew();

            Db.Insert(track);

            Subject.Clean();
            AllStoredModels.Where(x => x.BookId == 1).Should().HaveCount(1);

            Db.All<Book>().Should().Contain(e => e.BookFileId == AllStoredModels.First().Id);
        }
    }
}
