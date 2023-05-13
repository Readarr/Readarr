using System.Linq;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Indexers.Newznab;
using NzbDrone.Core.IndexerSearch.Definitions;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.IndexerTests.NewznabTests
{
    public class NewznabRequestGeneratorFixture : CoreTest<NewznabRequestGenerator>
    {
        private BookSearchCriteria _singleBookSearchCriteria;
        private NewznabCapabilities _capabilities;

        [SetUp]
        public void SetUp()
        {
            Subject.Settings = new NewznabSettings()
            {
                BaseUrl = "http://127.0.0.1:1234/",
                Categories = new[] { 1, 2 },
                ApiKey = "abcd",
            };

            _singleBookSearchCriteria = new BookSearchCriteria
            {
                Author = new Books.Author { Name = "Alien Ant Farm" },
                BookTitle = "TruANT"
            };

            _capabilities = new NewznabCapabilities();

            Mocker.GetMock<INewznabCapabilitiesProvider>()
                .Setup(v => v.GetCapabilities(It.IsAny<NewznabSettings>()))
                .Returns(_capabilities);
        }

        [Test]
        public void should_use_all_categories_for_feed()
        {
            var results = Subject.GetRecentRequests();

            results.GetAllTiers().Should().HaveCount(1);

            var page = results.GetAllTiers().First().First();

            page.Url.Query.Should().Contain("&cat=1,2&");
        }

        [Test]
        [Ignore("Disabled since no usenet indexers seem to support it")]
        public void should_search_by_author_and_book_if_supported()
        {
            _capabilities.SupportedBookSearchParameters = new[] { "q", "author", "title" };

            var results = Subject.GetSearchRequests(_singleBookSearchCriteria);
            results.GetTier(0).Should().HaveCount(1);

            var page = results.GetAllTiers().First().First();

            page.Url.Query.Should().Contain("author=Alien%20Ant%20Farm");
            page.Url.Query.Should().Contain("title=TruANT");
        }

        [Test]
        [Ignore("TODO: add raw search support")]
        public void should_encode_raw_title()
        {
            _capabilities.SupportedBookSearchParameters = new[] { "q", "author", "title" };

            // _capabilities.BookTextSearchEngine = "raw";
            _singleBookSearchCriteria.BookTitle = "Daisy Jones & The Six";

            var results = Subject.GetSearchRequests(_singleBookSearchCriteria);
            results.Tiers.Should().Be(1);

            var pageTier = results.GetTier(0).First().First();

            pageTier.Url.Query.Should().Contain("q=Daisy%20Jones%20%26%20The%20Six");
            pageTier.Url.Query.Should().NotContain(" & ");
            pageTier.Url.Query.Should().Contain("%26");
        }

        [Test]
        public void should_use_clean_title_and_encode()
        {
            _capabilities.SupportedBookSearchParameters = new[] { "q", "author", "title" };

            // _capabilities.BookTextSearchEngine = "sphinx";
            _singleBookSearchCriteria.BookTitle = "Daisy Jones & The Six";

            var results = Subject.GetSearchRequests(_singleBookSearchCriteria);
            results.Tiers.Should().Be(2);

            var pageTier = results.GetTier(0).First().First();

            pageTier.Url.Query.Should().Contain("q=Daisy%20Jones%20The%20Six");
            pageTier.Url.Query.Should().NotContain("and");
            pageTier.Url.Query.Should().NotContain(" & ");
            pageTier.Url.Query.Should().NotContain("%26");
        }
    }
}
