using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.OrganizerTests.FileNameBuilderTests
{
    [TestFixture]
    public class ColonReplacementFixture : CoreTest<FileNameBuilder>
    {
        private Author _author;
        private Book _book;
        private Edition _edition;
        private BookFile _bookFile;
        private NamingConfig _namingConfig;

        [SetUp]
        public void Setup()
        {
            _author = Builder<Author>
                .CreateNew()
                .With(s => s.Name = "Christopher Hopper")
                .Build();

            var series = Builder<Series>
                .CreateNew()
                .With(x => x.Title = "Series: Ruins of the Earth")
                .Build();

            var seriesLink = Builder<SeriesBookLink>
                .CreateListOfSize(1)
                .All()
                .With(s => s.Position = "1-2")
                .With(s => s.Series = series)
                .BuildListOfNew();

            _book = Builder<Book>
                .CreateNew()
                .With(s => s.Title = "Fake: Phantom Deadfall")
                .With(s => s.AuthorMetadata = _author.Metadata.Value)
                .With(s => s.ReleaseDate = new DateTime(2021, 2, 14))
                .With(s => s.SeriesLinks = seriesLink)
                .Build();

            _edition = Builder<Edition>
                .CreateNew()
                .With(s => s.Monitored = true)
                .With(s => s.Book = _book)
                .With(s => s.Title = _book.Title)
                .With(s => s.ReleaseDate = new DateTime(2021, 2, 17))
                .Build();

            _bookFile = new BookFile { Quality = new QualityModel(Quality.EPUB), ReleaseGroup = "ReadarrTest" };

            _namingConfig = NamingConfig.Default;
            _namingConfig.RenameBooks = true;

            Mocker.GetMock<INamingConfigService>()
                  .Setup(c => c.GetConfig()).Returns(_namingConfig);

            Mocker.GetMock<IQualityDefinitionService>()
                .Setup(v => v.Get(Moq.It.IsAny<Quality>()))
                .Returns<Quality>(v => Quality.DefaultQualityDefinitions.First(c => c.Quality == v));

            Mocker.GetMock<ICustomFormatService>()
                  .Setup(v => v.All())
                  .Returns(new List<CustomFormat>());
        }

        [Test]
        public void should_replace_colon_followed_by_space_with_space_dash_space_by_default()
        {
            _namingConfig.StandardBookFormat = "{Author Name} - {Book SeriesTitle - }{Book Title} {(Release Year)}";

            Subject.BuildBookFileName(_author, _edition, _bookFile)
                   .Should().Be("Christopher Hopper - Series - Ruins of the Earth #1-2 - Fake - Phantom Deadfall (2021)");
        }

        [TestCase("Fake: Phantom Deadfall", ColonReplacementFormat.Smart, "Christopher Hopper - Series - Ruins of the Earth - Fake - Phantom Deadfall (2021)")]
        [TestCase("Fake: Phantom Deadfall", ColonReplacementFormat.Dash, "Christopher Hopper - Series- Ruins of the Earth - Fake- Phantom Deadfall (2021)")]
        [TestCase("Fake: Phantom Deadfall", ColonReplacementFormat.Delete, "Christopher Hopper - Series Ruins of the Earth - Fake Phantom Deadfall (2021)")]
        [TestCase("Fake: Phantom Deadfall", ColonReplacementFormat.SpaceDash, "Christopher Hopper - Series - Ruins of the Earth - Fake - Phantom Deadfall (2021)")]
        [TestCase("Fake: Phantom Deadfall", ColonReplacementFormat.SpaceDashSpace, "Christopher Hopper - Series - Ruins of the Earth - Fake - Phantom Deadfall (2021)")]
        public void should_replace_colon_followed_by_space_with_expected_result(string bookTitle, ColonReplacementFormat replacementFormat, string expected)
        {
            _book.Title = bookTitle;
            _namingConfig.StandardBookFormat = "{Author Name} - {Book Series - }{Book Title} {(Release Year)}";
            _namingConfig.ColonReplacementFormat = replacementFormat;

            Subject.BuildBookFileName(_author, _edition, _bookFile)
                .Should().Be(expected);
        }

        [TestCase("Author:Name", ColonReplacementFormat.Smart, "Author-Name")]
        [TestCase("Author:Name", ColonReplacementFormat.Dash, "Author-Name")]
        [TestCase("Author:Name", ColonReplacementFormat.Delete, "AuthorName")]
        [TestCase("Author:Name", ColonReplacementFormat.SpaceDash, "Author -Name")]
        [TestCase("Author:Name", ColonReplacementFormat.SpaceDashSpace, "Author - Name")]
        public void should_replace_colon_with_expected_result(string authorName, ColonReplacementFormat replacementFormat, string expected)
        {
            _author.Name = authorName;
            _namingConfig.StandardBookFormat = "{Author Name}";
            _namingConfig.ColonReplacementFormat = replacementFormat;

            Subject.BuildBookFileName(_author, _edition, _bookFile)
                .Should().Be(expected);
        }
    }
}
