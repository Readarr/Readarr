using System.Collections.Generic;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Blocklisting;
using NzbDrone.Core.Books;
using NzbDrone.Core.Housekeeping.Housekeepers;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Housekeeping.Housekeepers
{
    [TestFixture]
    public class CleanupOrphanedBlocklistFixture : DbTest<CleanupOrphanedBlocklist, Blocklist>
    {
        [Test]
        public void should_delete_orphaned_blocklist_items()
        {
            var blocklist = Builder<Blocklist>.CreateNew()
                                              .With(h => h.BookIds = new List<int>())
                                              .With(h => h.Quality = new QualityModel())
                                              .BuildNew();

            Db.Insert(blocklist);
            Subject.Clean();
            AllStoredModels.Should().BeEmpty();
        }

        [Test]
        public void should_not_delete_unorphaned_blocklist_items()
        {
            var author = Builder<Author>.CreateNew().BuildNew();

            Db.Insert(author);

            var blocklist = Builder<Blocklist>.CreateNew()
                                              .With(h => h.BookIds = new List<int>())
                                              .With(h => h.Quality = new QualityModel())
                                              .With(b => b.AuthorId = author.Id)
                                              .BuildNew();

            Db.Insert(blocklist);

            Subject.Clean();
            AllStoredModels.Should().HaveCount(1);
        }
    }
}
