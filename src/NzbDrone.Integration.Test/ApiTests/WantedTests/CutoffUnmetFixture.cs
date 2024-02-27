using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.Qualities;
using Readarr.Api.V1.RootFolders;

namespace NzbDrone.Integration.Test.ApiTests.WantedTests
{
    [TestFixture]
    [Ignore("Waiting for metadata to be back again", Until = "2024-03-15 00:00:00Z")]
    public class CutoffUnmetFixture : IntegrationTest
    {
        [SetUp]
        public void Setup()
        {
            // Add a root folder
            RootFolders.Post(new RootFolderResource
            {
                Name = "TestLibrary",
                Path = AuthorRootFolder,
                DefaultMetadataProfileId = 1,
                DefaultQualityProfileId = 1,
                DefaultMonitorOption = MonitorTypes.All
            });
        }

        [Test]
        [Order(2)]
        public void cutoff_should_have_monitored_items()
        {
            EnsureProfileCutoff(1, Quality.AZW3, true);
            var author = EnsureAuthor("14586394", "43765115", "Andrew Hunter Murray", true);
            EnsureBookFile(author, 1, "43765115", Quality.MOBI);

            var result = WantedCutoffUnmet.GetPaged(0, 15, "releaseDate", "desc");

            result.Records.Should().NotBeEmpty();
        }

        [Test]
        [Order(2)]
        public void cutoff_should_not_have_unmonitored_items()
        {
            EnsureProfileCutoff(1, Quality.AZW3, true);
            var author = EnsureAuthor("14586394", "43765115", "Andrew Hunter Murray", false);
            EnsureBookFile(author, 1, "43765115", Quality.MOBI);

            var result = WantedCutoffUnmet.GetPaged(0, 15, "releaseDate", "desc");

            result.Records.Should().BeEmpty();
        }

        [Test]
        [Order(2)]
        public void cutoff_should_have_author()
        {
            EnsureProfileCutoff(1, Quality.AZW3, true);
            var author = EnsureAuthor("14586394", "43765115", "Andrew Hunter Murray", true);
            EnsureBookFile(author, 1, "43765115", Quality.MOBI);

            var result = WantedCutoffUnmet.GetPagedIncludeAuthor(0, 15, "releaseDate", "desc", includeAuthor: true);

            result.Records.First().Author.Should().NotBeNull();
            result.Records.First().Author.AuthorName.Should().Be("Andrew Hunter Murray");
        }

        [Test]
        [Order(2)]
        public void cutoff_should_not_have_author()
        {
            EnsureProfileCutoff(1, Quality.AZW3, true);
            var author = EnsureAuthor("14586394", "43765115", "Andrew Hunter Murray", true);
            EnsureBookFile(author, 1, "43765115", Quality.MOBI);

            var result = WantedCutoffUnmet.GetPagedIncludeAuthor(0, 15, "releaseDate", "desc", includeAuthor: false);

            result.Records.First().Author.Should().BeNull();
        }

        [Test]
        [Order(2)]
        public void cutoff_should_have_unmonitored_items()
        {
            EnsureProfileCutoff(1, Quality.AZW3, true);
            var author = EnsureAuthor("14586394", "43765115", "Andrew Hunter Murray", false);
            EnsureBookFile(author, 1, "43765115", Quality.MOBI);

            var result = WantedCutoffUnmet.GetPaged(0, 15, "releaseDate", "desc", "monitored", false);

            result.Records.Should().NotBeEmpty();
        }
    }
}
