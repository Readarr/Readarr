using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.OrganizerTests.FileNameBuilderTests
{
    [TestFixture]
    public class TruncatedBookTitlesFixture : CoreTest<FileNameBuilder>
    {
        private BookFile _bookFile;
        private NamingConfig _namingConfig;

        [SetUp]
        public void Setup()
        {
            _namingConfig = NamingConfig.Default;
            _namingConfig.RenameBooks = true;

            Mocker.GetMock<INamingConfigService>()
                .Setup(c => c.GetConfig()).Returns(_namingConfig);

            _bookFile = new BookFile { Quality = new QualityModel(Quality.EPUB), ReleaseGroup = "ReadarrTest" };

            Mocker.GetMock<IQualityDefinitionService>()
                .Setup(v => v.Get(Moq.It.IsAny<Quality>()))
                .Returns<Quality>(v => Quality.DefaultQualityDefinitions.First(c => c.Quality == v));
        }

        private (Author, Edition) BuildTestInputs(string authorName, string bookTitle, string seriesName, string seriesNumber)
        {
            var author = Builder<Author>
                .CreateNew()
                .With(s => s.Name = authorName)
                .Build();

            var series = Builder<Series>
                .CreateNew()
                .With(x => x.Title = seriesName)
                .Build();

            var seriesLink = Builder<SeriesBookLink>
                .CreateListOfSize(1)
                .All()
                .With(s => s.Position = seriesNumber)
                .With(s => s.Series = series)
                .BuildListOfNew();

            var book = Builder<Book>
                .CreateNew()
                .With(s => s.Title = bookTitle)
                .With(s => s.AuthorMetadata = author.Metadata.Value)
                .With(s => s.SeriesLinks = seriesLink)
                .Build();

            var edition = Builder<Edition>
                .CreateNew()
                .With(s => s.Title = book.Title)
                .With(s => s.Book = book)
                .Build();

            return (author, edition);
        }

        [Test]
        public void should_not_truncate_filename_if_length_is_less_than_max_length_limit_long_book_name()
        {
            var authorName = "Brandon Sanderson";
            var bookTitle = "Knights of Wind and Truth and Several Extra Words to Get The Length Close to 255 Characters in Total So That We Can Test Whether Or Not The Filename Is Truncated But The Length Is Below the Allowed Maximum";
            var seriesName = "The Stormlight Archive";
            var seriesNumber = "5";
            var expected = "Brandon Sanderson - The Stormlight Archive #5 - Knights of Wind and Truth and Several Extra Words to Get The Length Close to 255 Characters in Total So That We Can Test Whether Or Not The Filename Is Truncated But The Length Is Below the Allowed Maximum";
            _namingConfig.StandardBookFormat = "{Author Name} - {Book SeriesTitle} - {Book Title}";
            var (author, edition) = BuildTestInputs(authorName, bookTitle, seriesName, seriesNumber);

            var result = Subject.BuildBookFileName(author, edition, _bookFile);
            result.Should().Be(expected);
            result.Length.Should().BeLessThan(255);
        }

        [Test]
        public void should_not_truncate_filename_if_length_is_less_than_max_length_limit_long_author_name()
        {
            var authorName = "Brandon Sanderson and Several Extra Words to Get The Length Close to 255 Characters in Total So That We Can Test Whether Or Not The Filename Is Truncated But The Length Is Below the Allowed Maximum";
            var bookTitle = "Knights of Wind and Truth";
            var seriesName = "The Stormlight Archive";
            var seriesNumber = "5";
            var expected = "Brandon Sanderson and Several Extra Words to Get The Length Close to 255 Characters in Total So That We Can Test Whether Or Not The Filename Is Truncated But The Length Is Below the Allowed Maximum - The Stormlight Archive #5 - Knights of Wind and Truth";
            _namingConfig.StandardBookFormat = "{Author Name} - {Book SeriesTitle} - {Book Title}";
            var (author, edition) = BuildTestInputs(authorName, bookTitle, seriesName, seriesNumber);

            var result = Subject.BuildBookFileName(author, edition, _bookFile);
            result.Should().Be(expected);
            result.Length.Should().BeLessThan(255);
        }

        [Test]
        public void should_not_truncate_filename_if_length_is_less_than_max_length_limit_long_series_name()
        {
            var authorName = "Brandon Sanderson";
            var bookTitle = "Knights of Wind and Truth";
            var seriesName = "The Stormlight Archive and Several Extra Words to Get The Length Close to 255 Characters in Total So That We Can Test Whether Or Not The Filename Is Truncated But The Length Is Below the Allowed Maximum";
            var seriesNumber = "5";
            var expected = "Brandon Sanderson - The Stormlight Archive and Several Extra Words to Get The Length Close to 255 Characters in Total So That We Can Test Whether Or Not The Filename Is Truncated But The Length Is Below the Allowed Maximum #5 - Knights of Wind and Truth";
            _namingConfig.StandardBookFormat = "{Author Name} - {Book SeriesTitle} - {Book Title}";
            var (author, edition) = BuildTestInputs(authorName, bookTitle, seriesName, seriesNumber);

            var result = Subject.BuildBookFileName(author, edition, _bookFile);
            result.Should().Be(expected);
            result.Length.Should().BeLessThan(255);
        }

        [Test]
        public void should_not_truncate_filename_if_length_is_equal_to_than_max_length_limit_long_book_name()
        {
            var authorName = "Brandon Sanderson";
            var bookTitle = "Knights of Wind and Truth and Several Extra Words to Get The Length Close to 255 Characters in Total So That We Can Test Whether Or Not The Filename Is Truncated But The Length Is Below the Allowed Maximum";
            var seriesName = "The Stormlight Archive";
            var seriesNumber = "5-1";
            var expected = "Brandon Sanderson - The Stormlight Archive #5-1 - Knights of Wind and Truth and Several Extra Words to Get The Length Close to 255 Characters in Total So That We Can Test Whether Or Not The Filename Is Truncated But The Length Is Below the Allowed Maximum";
            _namingConfig.StandardBookFormat = "{Author Name} - {Book SeriesTitle} - {Book Title}";
            var (author, edition) = BuildTestInputs(authorName, bookTitle, seriesName, seriesNumber);

            var result = Subject.BuildBookFileName(author, edition, _bookFile);
            result.Should().Be(expected);
            result.Length.Should().Be(255);
        }

        [Test]
        public void should_not_truncate_filename_if_length_is_equal_to_than_max_length_limit_long_author_name()
        {
            var authorName = "Brandon Sanderson and Several Extra Words to Get The Length Close to 255 Characters in Total So That We Can Test Whether Or Not The Filename Is Truncated But The Length Is Below the Allowed Maximum";
            var bookTitle = "Knights of Wind and Truth";
            var seriesName = "The Stormlight Archive";
            var seriesNumber = "5-1";
            var expected = "Brandon Sanderson and Several Extra Words to Get The Length Close to 255 Characters in Total So That We Can Test Whether Or Not The Filename Is Truncated But The Length Is Below the Allowed Maximum - The Stormlight Archive #5-1 - Knights of Wind and Truth";
            _namingConfig.StandardBookFormat = "{Author Name} - {Book SeriesTitle} - {Book Title}";
            var (author, edition) = BuildTestInputs(authorName, bookTitle, seriesName, seriesNumber);

            var result = Subject.BuildBookFileName(author, edition, _bookFile);
            result.Should().Be(expected);
            result.Length.Should().Be(255);
        }

        [Test]
        public void should_not_truncate_filename_if_length_is_equal_to_than_max_length_limit_long_series_name()
        {
            var authorName = "Brandon Sanderson";
            var bookTitle = "Knights of Wind and Truth";
            var seriesName = "The Stormlight Archive and Several Extra Words to Get The Length Close to 255 Characters in Total So That We Can Test Whether Or Not The Filename Is Truncated But The Length Is Below the Allowed Maximum";
            var seriesNumber = "5-1";
            var expected = "Brandon Sanderson - The Stormlight Archive and Several Extra Words to Get The Length Close to 255 Characters in Total So That We Can Test Whether Or Not The Filename Is Truncated But The Length Is Below the Allowed Maximum #5-1 - Knights of Wind and Truth";
            _namingConfig.StandardBookFormat = "{Author Name} - {Book SeriesTitle} - {Book Title}";
            var (author, edition) = BuildTestInputs(authorName, bookTitle, seriesName, seriesNumber);

            var result = Subject.BuildBookFileName(author, edition, _bookFile);
            result.Should().Be(expected);
            result.Length.Should().Be(255);
        }

        [Test]
        public void should_truncate_filename_if_length_is_greater_than_max_length_limit_long_book_name()
        {
            var authorName = "Brandon Sanderson and Janci Patterson";
            var bookTitle = "Knights of Wind and Truth and Several Extra Words to Get The Length Close to 255 Characters in Total So That We Can Test Whether Or Not The Filename Is Truncated But The Length Is Below the Allowed Maximum";
            var seriesName = "The Stormlight Archive";
            var seriesNumber = "5";
            var expected = "Brandon Sanderson and Janci Patterson - The Stormlight Archive #5 - Knights of Wind and Truth and Several Extra Words to Get The Length Close to 255 Characters in Total So That We Can Test Whether Or Not The Filename Is Truncated But The Length Is Belo...";
            _namingConfig.StandardBookFormat = "{Author Name} - {Book SeriesTitle} - {Book Title}";
            var (author, edition) = BuildTestInputs(authorName, bookTitle, seriesName, seriesNumber);

            var result = Subject.BuildBookFileName(author, edition, _bookFile);
            result.Should().Be(expected);
            result.Length.Should().Be(255);
        }

        [Test]
        public void should_truncate_filename_if_length_is_greater_than_than_max_length_limit_long_author_name()
        {
            var authorName = "Brandon Sanderson and Janci Patterson and Several Extra Words to Get The Length Close to 255 Characters in Total So That We Can Test Whether Or Not The Filename Is Truncated But The Length Is Below the Allowed Maximum";
            var bookTitle = "Knights of Wind and Truth";
            var seriesName = "The Stormlight Archive";
            var seriesNumber = "5";
            var expected = "Brandon Sanderson and Janci Patterson and Several Extra Words to Get The Length Close to 255 Characters in Total So That We Can Test Whether Or Not The Filename Is Truncated But The Length Is Below the Allowed Maximum - The Stormlight Archive #5 - Knig...";
            _namingConfig.StandardBookFormat = "{Author Name} - {Book SeriesTitle} - {Book Title}";
            var (author, edition) = BuildTestInputs(authorName, bookTitle, seriesName, seriesNumber);

            var result = Subject.BuildBookFileName(author, edition, _bookFile);
            result.Should().Be(expected);
            result.Length.Should().Be(255);
        }

        [Test]
        public void should_truncate_filename_if_length_is_greater_than_than_max_length_limit_long_series_name()
        {
            var authorName = "Brandon Sanderson and Janci Patterson";
            var bookTitle = "Knights of Wind and Truth";
            var seriesName = "The Stormlight Archive and Several Extra Words to Get The Length Close to 255 Characters in Total So That We Can Test Whether Or Not The Filename Is Truncated But The Length Is Below the Allowed Maximum";
            var seriesNumber = "5";
            var expected = "Brandon Sanderson and Janci Patterson - The Stormlight Archive and Several Extra Words to Get The Length Close to 255 Characters in Total So That We Can Test Whether Or Not The Filename Is Truncated But The Length Is Below the Allowed Maximum #5 - Knig...";
            _namingConfig.StandardBookFormat = "{Author Name} - {Book SeriesTitle} - {Book Title}";
            var (author, edition) = BuildTestInputs(authorName, bookTitle, seriesName, seriesNumber);

            var result = Subject.BuildBookFileName(author, edition, _bookFile);
            result.Should().Be(expected);
            result.Length.Should().Be(255);
        }
    }
}
