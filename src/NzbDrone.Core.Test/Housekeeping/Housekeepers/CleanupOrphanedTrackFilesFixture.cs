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
    }
}
