using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.AuthorStats;
using NzbDrone.Core.Books;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.AuthorStatsTests
{
    [TestFixture]
    public class AuthorStatisticsFixture : DbTest<AuthorStatisticsRepository, Author>
    {
        private Author _author;
        private Book _book;
        private Edition _edition;
        private List<BookFile> _bookFiles;

        [SetUp]
        public void Setup()
        {
            _author = Builder<Author>.CreateNew()
                .With(a => a.AuthorMetadataId = 10)
                .BuildNew();
            Db.Insert(_author);

            _book = Builder<Book>.CreateNew()
                .With(e => e.ReleaseDate = DateTime.Today.AddDays(-5))
                .With(e => e.AuthorMetadataId = 10)
                .BuildNew();
            Db.Insert(_book);

            _edition = Builder<Edition>.CreateNew()
                .With(e => e.BookId = _book.Id)
                .With(e => e.Monitored = true)
                .BuildNew();
            Db.Insert(_edition);

            _bookFiles = Builder<BookFile>.CreateListOfSize(2)
                .All()
                .With(x => x.Id = 0)
                .With(e => e.Author = _author)
                .With(e => e.Edition = _edition)
                .With(e => e.EditionId = _edition.Id)
                .With(e => e.Quality = new QualityModel(Quality.MP3))
                .BuildList();
        }

        private void GivenBookFile()
        {
            Db.Insert(_bookFiles[0]);
        }

        private void GivenTwoBookFiles()
        {
            Db.InsertMany(_bookFiles);
        }

        [Test]
        public void should_get_stats_for_author()
        {
            var stats = Subject.AuthorStatistics();

            stats.Should().HaveCount(1);
        }

        [Test]
        public void should_not_include_unmonitored_book_in_book_count()
        {
            var stats = Subject.AuthorStatistics();

            stats.Should().HaveCount(1);
            stats.First().BookCount.Should().Be(0);
        }

        [Test]
        public void should_include_unmonitored_book_with_file_in_book_count()
        {
            GivenBookFile();

            var stats = Subject.AuthorStatistics();

            stats.Should().HaveCount(1);
            stats.First().BookCount.Should().Be(1);
        }

        [Test]
        public void should_have_size_on_disk_of_zero_when_no_book_file()
        {
            var stats = Subject.AuthorStatistics();

            stats.Should().HaveCount(1);
            stats.First().SizeOnDisk.Should().Be(0);
        }

        [Test]
        public void should_have_size_on_disk_when_book_file_exists()
        {
            GivenBookFile();

            var stats = Subject.AuthorStatistics();

            stats.Should().HaveCount(1);
            stats.First().SizeOnDisk.Should().Be(_bookFiles[0].Size);
        }

        [Test]
        public void should_count_book_with_two_files_as_one_book()
        {
            GivenTwoBookFiles();

            var stats = Subject.AuthorStatistics();

            Db.All<BookFile>().Should().HaveCount(2);
            stats.Should().HaveCount(1);

            var bookStats = stats.First();

            bookStats.TotalBookCount.Should().Be(1);
            bookStats.BookCount.Should().Be(1);
            bookStats.AvailableBookCount.Should().Be(1);
            bookStats.SizeOnDisk.Should().Be(_bookFiles.Sum(x => x.Size));
            bookStats.BookFileCount.Should().Be(2);
        }
    }
}
