using System.Collections;
using System.Linq;
using System.Reflection;
using AutoFixture;
using Equ;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Music;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MusicTests
{
    [TestFixture]
    public class EntityFixture : LoggingTest
    {
        private Fixture _fixture = new Fixture();

        private static bool IsNotMarkedAsIgnore(PropertyInfo propertyInfo)
        {
            return !propertyInfo.GetCustomAttributes(typeof(MemberwiseEqualityIgnoreAttribute), true).Any();
        }

        public class EqualityPropertySource<T>
        {
            public static IEnumerable TestCases
            {
                get
                {
                    foreach (var property in typeof(T).GetProperties().Where(x => x.CanRead && x.CanWrite && IsNotMarkedAsIgnore(x)))
                    {
                        yield return new TestCaseData(property).SetName($"{{m}}_{property.Name}");
                    }
                }
            }
        }

        public class IgnoredPropertySource<T>
        {
            public static IEnumerable TestCases
            {
                get
                {
                    foreach (var property in typeof(T).GetProperties().Where(x => x.CanRead && x.CanWrite && !IsNotMarkedAsIgnore(x)))
                    {
                        yield return new TestCaseData(property).SetName($"{{m}}_{property.Name}");
                    }
                }
            }
        }

        [Test]
        public void two_equivalent_artist_metadata_should_be_equal()
        {
            var item1 = _fixture.Create<AuthorMetadata>();
            var item2 = item1.JsonClone();

            item1.Should().NotBeSameAs(item2);
            item1.Should().Be(item2);
        }

        [Test]
        [TestCaseSource(typeof(EqualityPropertySource<AuthorMetadata>), "TestCases")]
        public void two_different_artist_metadata_should_not_be_equal(PropertyInfo prop)
        {
            var item1 = _fixture.Create<AuthorMetadata>();
            var item2 = item1.JsonClone();
            var different = _fixture.Create<AuthorMetadata>();

            // make item2 different in the property under consideration
            var differentEntry = prop.GetValue(different);
            prop.SetValue(item2, differentEntry);

            item1.Should().NotBeSameAs(item2);
            item1.Should().NotBe(item2);
        }

        [Test]
        public void metadata_and_db_fields_should_replicate_artist_metadata()
        {
            var item1 = _fixture.Create<AuthorMetadata>();
            var item2 = _fixture.Create<AuthorMetadata>();

            item1.Should().NotBe(item2);

            item1.UseMetadataFrom(item2);
            item1.UseDbFieldsFrom(item2);
            item1.Should().Be(item2);
        }

        private Book GivenAlbum()
        {
            return _fixture.Build<Book>()
                .Without(x => x.AuthorMetadata)
                .Without(x => x.Author)
                .Without(x => x.AuthorId)
                .Create();
        }

        [Test]
        public void two_equivalent_albums_should_be_equal()
        {
            var item1 = GivenAlbum();
            var item2 = item1.JsonClone();

            item1.Should().NotBeSameAs(item2);
            item1.Should().Be(item2);
        }

        [Test]
        [TestCaseSource(typeof(EqualityPropertySource<Book>), "TestCases")]
        public void two_different_albums_should_not_be_equal(PropertyInfo prop)
        {
            var item1 = GivenAlbum();
            var item2 = item1.JsonClone();
            var different = GivenAlbum();

            // make item2 different in the property under consideration
            if (prop.PropertyType == typeof(bool))
            {
                prop.SetValue(item2, !(bool)prop.GetValue(item1));
            }
            else
            {
                prop.SetValue(item2, prop.GetValue(different));
            }

            item1.Should().NotBeSameAs(item2);
            item1.Should().NotBe(item2);
        }

        [Test]
        public void metadata_and_db_fields_should_replicate_album()
        {
            var item1 = GivenAlbum();
            var item2 = GivenAlbum();

            item1.Should().NotBe(item2);

            item1.UseMetadataFrom(item2);
            item1.UseDbFieldsFrom(item2);
            item1.Should().Be(item2);
        }

        private Author GivenArtist()
        {
            return _fixture.Build<Author>()
                .With(x => x.Metadata, new LazyLoaded<AuthorMetadata>(_fixture.Create<AuthorMetadata>()))
                .Without(x => x.QualityProfile)
                .Without(x => x.MetadataProfile)
                .Without(x => x.Books)
                .Without(x => x.Name)
                .Without(x => x.ForeignAuthorId)
                .Create();
        }

        [Test]
        public void two_equivalent_artists_should_be_equal()
        {
            var item1 = GivenArtist();
            var item2 = item1.JsonClone();

            item1.Should().NotBeSameAs(item2);
            item1.Should().Be(item2);
        }

        [Test]
        [TestCaseSource(typeof(EqualityPropertySource<Author>), "TestCases")]
        public void two_different_artists_should_not_be_equal(PropertyInfo prop)
        {
            var item1 = GivenArtist();
            var item2 = item1.JsonClone();
            var different = GivenArtist();

            // make item2 different in the property under consideration
            if (prop.PropertyType == typeof(bool))
            {
                prop.SetValue(item2, !(bool)prop.GetValue(item1));
            }
            else
            {
                prop.SetValue(item2, prop.GetValue(different));
            }

            item1.Should().NotBeSameAs(item2);
            item1.Should().NotBe(item2);
        }

        [Test]
        public void metadata_and_db_fields_should_replicate_artist()
        {
            var item1 = GivenArtist();
            var item2 = GivenArtist();

            item1.Should().NotBe(item2);

            item1.UseMetadataFrom(item2);
            item1.UseDbFieldsFrom(item2);
            item1.Should().Be(item2);
        }
    }
}
