using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.OrganizerTests.FileNameBuilderTests
{
    [TestFixture]
    public class NestedFileNameBuilderFixture : CoreTest<FileNameBuilder>
    {
        private Author _artist;
        private Book _album;
        private Edition _release;
        private BookFile _trackFile;
        private NamingConfig _namingConfig;

        [SetUp]
        public void Setup()
        {
            _artist = Builder<Author>
                    .CreateNew()
                    .With(s => s.Name = "AuthorName")
                    .With(s => s.Metadata = new AuthorMetadata
                    {
                        Disambiguation = "US Author",
                        Name = "AuthorName"
                    })
                    .Build();

            _album = Builder<Book>
                .CreateNew()
                .With(s => s.Author = _artist)
                .With(s => s.AuthorMetadata = _artist.Metadata.Value)
                .With(s => s.Title = "A Novel")
                .With(s => s.ReleaseDate = new DateTime(2020, 1, 15))
                .With(s => s.SeriesLinks = new List<SeriesBookLink>())
                .Build();

            _release = Builder<Edition>
                .CreateNew()
                .With(s => s.Monitored = true)
                .With(s => s.Book = _album)
                .With(s => s.Title = "A Novel")
                .With(s => s.ReleaseDate = new DateTime(2020, 1, 15))
                .Build();

            _namingConfig = NamingConfig.Default;
            _namingConfig.RenameBooks = true;

            Mocker.GetMock<INamingConfigService>()
                  .Setup(c => c.GetConfig()).Returns(_namingConfig);

            _trackFile = Builder<BookFile>.CreateNew()
                .With(e => e.Quality = new QualityModel(Quality.MOBI))
                .With(e => e.ReleaseGroup = "ReadarrTest")
                .Build();

            Mocker.GetMock<IQualityDefinitionService>()
                .Setup(v => v.Get(Moq.It.IsAny<Quality>()))
                .Returns<Quality>(v => Quality.DefaultQualityDefinitions.First(c => c.Quality == v));
        }

        private void WithSeries()
        {
            _album.SeriesLinks = new List<SeriesBookLink>
            {
                new SeriesBookLink
                {
                    Series = new Series
                    {
                        Title = "A Series",
                    },
                    Position = "2-3",
                    SeriesPosition = 1
                }
            };
        }

        [Test]
        public void should_build_nested_standard_track_filename_with_forward_slash()
        {
            WithSeries();

            _namingConfig.StandardBookFormat = "{Book Series}/{Book SeriesTitle - }{Book Title} {(Release Year)}";

            var name = Subject.BuildBookFileName(_artist, _release, _trackFile)
                .Should().Be("A Series\\A Series #2-3 - A Novel (2020)".AsOsAgnostic());
        }

        [Test]
        public void should_build_standard_track_filename_with_forward_slash()
        {
            _namingConfig.StandardBookFormat = "{Book Series}/{Book SeriesTitle - }{Book Title} {(Release Year)}";

            Subject.BuildBookFileName(_artist, _release, _trackFile)
                .Should().Be("A Novel (2020)".AsOsAgnostic());
        }

        [Test]
        public void should_build_nested_standard_track_filename_with_back_slash()
        {
            WithSeries();

            _namingConfig.StandardBookFormat = "{Book Series}\\{Book SeriesTitle - }{Book Title} {(Release Year)}";

            Subject.BuildBookFileName(_artist, _release, _trackFile)
                   .Should().Be("A Series\\A Series #2-3 - A Novel (2020)".AsOsAgnostic());
        }

        [Test]
        public void should_build_standard_track_filename_with_back_slash()
        {
            _namingConfig.StandardBookFormat = "{Book Series}\\{Book SeriesTitle - }{Book Title} {(Release Year)}";

            Subject.BuildBookFileName(_artist, _release, _trackFile)
                .Should().Be("A Novel (2020)".AsOsAgnostic());
        }
    }
}
