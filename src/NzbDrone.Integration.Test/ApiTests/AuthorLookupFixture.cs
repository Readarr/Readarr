using FluentAssertions;
using NUnit.Framework;

namespace NzbDrone.Integration.Test.ApiTests
{
    [TestFixture]
    [Ignore("Waiting for metadata to be back again", Until = "2024-05-15 00:00:00Z")]
    public class AuthorLookupFixture : IntegrationTest
    {
        [TestCase("Robert Harris", "Robert Harris")]
        [TestCase("Philip W. Errington", "Philip W. Errington")]
        public void lookup_new_author_by_name(string term, string name)
        {
            var author = Author.Lookup(term);

            author.Should().NotBeEmpty();
            author.Should().Contain(c => c.AuthorName == name);
        }

        [Test]
        public void lookup_new_author_by_goodreads_book_id()
        {
            var author = Author.Lookup("edition:2");

            author.Should().NotBeEmpty();
            author.Should().Contain(c => c.AuthorName == "J.K. Rowling");
        }
    }
}
