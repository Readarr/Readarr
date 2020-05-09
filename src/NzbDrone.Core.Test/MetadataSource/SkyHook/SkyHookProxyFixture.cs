using System;
using System.Collections.Generic;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MetadataSource.SkyHook;
using NzbDrone.Core.Music;
using NzbDrone.Core.Profiles.Metadata;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.MetadataSource.SkyHook
{
    [TestFixture]
    public class SkyHookProxyFixture : CoreTest<SkyHookProxy>
    {
        private MetadataProfile _metadataProfile;

        [SetUp]
        public void Setup()
        {
            UseRealHttp();

            _metadataProfile = new MetadataProfile();

            Mocker.GetMock<IMetadataProfileService>()
                .Setup(s => s.Get(It.IsAny<int>()))
                .Returns(_metadataProfile);

            Mocker.GetMock<IMetadataProfileService>()
                .Setup(s => s.Exists(It.IsAny<int>()))
                .Returns(true);
        }

        [TestCase("amzn1.gr.author.v1.qTrNu9-PIaaBj5gYRDmN4Q", "Terry Pratchett")]
        [TestCase("amzn1.gr.author.v1.afCyJgprpWE2xJU2_z3zTQ", "Robert Harris")]
        public void should_be_able_to_get_author_detail(string mbId, string name)
        {
            var details = Subject.GetAuthorInfo(mbId);

            ValidateAuthor(details);

            details.Name.Should().Be(name);
        }

        [TestCase("amzn1.gr.book.v1.2rp8a0vJ8clGzMzZf61R9Q", "Guards! Guards!")]
        public void should_be_able_to_get_book_detail(string mbId, string name)
        {
            var details = Subject.GetBookInfo(mbId);

            ValidateAlbums(new List<Book> { details.Item2 });

            details.Item2.Title.Should().Be(name);
        }

        [Test]
        public void getting_details_of_invalid_artist()
        {
            Assert.Throws<ArtistNotFoundException>(() => Subject.GetAuthorInfo("66c66aaa-6e2f-4930-8610-912e24c63ed1"));
        }

        [Test]
        public void getting_details_of_invalid_album()
        {
            Assert.Throws<AlbumNotFoundException>(() => Subject.GetBookInfo("66c66aaa-6e2f-4930-8610-912e24c63ed1"));
        }

        private void ValidateAuthor(Author author)
        {
            author.Should().NotBeNull();
            author.Name.Should().NotBeNullOrWhiteSpace();
            author.CleanName.Should().Be(Parser.Parser.CleanArtistName(author.Name));
            author.SortName.Should().Be(Parser.Parser.NormalizeTitle(author.Name));
            author.Metadata.Value.TitleSlug.Should().NotBeNullOrWhiteSpace();
            author.Metadata.Value.Overview.Should().NotBeNullOrWhiteSpace();
            author.Metadata.Value.Images.Should().NotBeEmpty();
            author.ForeignAuthorId.Should().NotBeNullOrWhiteSpace();
            author.Books.IsLoaded.Should().BeTrue();
            author.Books.Value.Should().NotBeEmpty();
            author.Books.Value.Should().OnlyContain(x => x.CleanTitle != null);
        }

        private void ValidateAlbums(List<Book> albums, bool idOnly = false)
        {
            albums.Should().NotBeEmpty();

            foreach (var album in albums)
            {
                album.ForeignBookId.Should().NotBeNullOrWhiteSpace();
                if (!idOnly)
                {
                    ValidateAlbum(album);
                }
            }

            //if atleast one album has title it means parse it working.
            if (!idOnly)
            {
                albums.Should().Contain(c => !string.IsNullOrWhiteSpace(c.Title));
            }
        }

        private void ValidateAlbum(Book album)
        {
            album.Should().NotBeNull();

            album.Title.Should().NotBeNullOrWhiteSpace();

            album.Should().NotBeNull();

            if (album.ReleaseDate.HasValue)
            {
                album.ReleaseDate.Value.Kind.Should().Be(DateTimeKind.Utc);
            }
        }
    }
}
