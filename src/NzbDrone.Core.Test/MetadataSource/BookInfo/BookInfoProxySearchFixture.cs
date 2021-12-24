using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Http;
using NzbDrone.Core.Books;
using NzbDrone.Core.Http;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.MetadataSource.BookInfo;
using NzbDrone.Core.MetadataSource.Goodreads;
using NzbDrone.Core.Profiles.Metadata;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MetadataSource.Goodreads
{
    [TestFixture]
    public class BookInfoProxySearchFixture : CoreTest<BookInfoProxy>
    {
        [SetUp]
        public void Setup()
        {
            UseRealHttp();

            Mocker.SetConstant<IGoodreadsSearchProxy>(Mocker.Resolve<GoodreadsSearchProxy>());

            var httpClient = Mocker.Resolve<IHttpClient>();
            Mocker.GetMock<ICachedHttpResponseService>()
                .Setup(x => x.Get<List<SearchJsonResource>>(It.IsAny<HttpRequest>(), It.IsAny<bool>(), It.IsAny<TimeSpan>()))
                .Returns((HttpRequest request, bool useCache, TimeSpan ttl) => httpClient.Get<List<SearchJsonResource>>(request));

            var metadataProfile = new MetadataProfile();

            Mocker.GetMock<IMetadataProfileService>()
                .Setup(s => s.All())
                .Returns(new List<MetadataProfile> { metadataProfile });

            Mocker.GetMock<IMetadataProfileService>()
                .Setup(s => s.Get(It.IsAny<int>()))
                .Returns(metadataProfile);
        }

        [TestCase("Robert Harris", "Robert Harris")]
        [TestCase("James Patterson", "James Patterson")]
        [TestCase("Antoine de Saint-Exupéry", "Antoine de Saint-Exupéry")]
        public void successful_author_search(string title, string expected)
        {
            var result = Subject.SearchForNewAuthor(title);

            result.Should().NotBeEmpty();

            result[0].Name.Should().Be(expected);

            ExceptionVerification.IgnoreWarns();
        }

        [TestCase("Harry Potter and the sorcerer's stone", null, "Harry Potter and the Sorcerer's Stone")]
        [TestCase("edition:3", null, "Harry Potter and the Sorcerer's Stone")]
        [TestCase("edition: 3", null, "Harry Potter and the Sorcerer's Stone")]
        [TestCase("asin:B0192CTMYG", null, "Harry Potter and the Sorcerer's Stone")]
        [TestCase("isbn:9780439554930", null, "Harry Potter and the Sorcerer's Stone")]
        public void successful_book_search(string title, string author, string expected)
        {
            var result = Subject.SearchForNewBook(title, author);

            result.Should().NotBeEmpty();

            result[0].Editions.Value[0].Title.Should().Be(expected);

            ExceptionVerification.IgnoreWarns();
        }

        [TestCase("edition:")]
        [TestCase("edition: 99999999999999999999")]
        [TestCase("edition: 0")]
        [TestCase("edition: -12")]
        [TestCase("edition: aaaa")]
        [TestCase("adjalkwdjkalwdjklawjdlKAJD")]
        public void no_author_search_result(string term)
        {
            var result = Subject.SearchForNewAuthor(term);
            result.Should().BeEmpty();

            ExceptionVerification.IgnoreWarns();
        }

        [TestCase("Philip Pullman", 0, typeof(Author), "Philip Pullman")]
        [TestCase("Philip Pullman", 1, typeof(Book), "Northern Lights")]
        public void successful_combined_search(string query, int position, Type resultType, string expected)
        {
            var result = Subject.SearchForNewEntity(query);
            result.Should().NotBeEmpty();
            result[position].GetType().Should().Be(resultType);

            if (resultType == typeof(Author))
            {
                var cast = result[position] as Author;
                cast.Should().NotBeNull();
                cast.Name.Should().Be(expected);
            }
            else
            {
                var cast = result[position] as Book;
                cast.Should().NotBeNull();
                cast.Title.Should().Be(expected);
            }
        }
    }
}
