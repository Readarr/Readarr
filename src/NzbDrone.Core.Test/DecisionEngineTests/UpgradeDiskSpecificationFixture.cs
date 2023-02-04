using System;
using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.CustomFormats;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.DecisionEngineTests
{
    [TestFixture]
    [Ignore("Pending Readarr fixes")]
    public class UpgradeDiskSpecificationFixture : CoreTest<UpgradeDiskSpecification>
    {
        private RemoteBook _parseResultMulti;
        private RemoteBook _parseResultSingle;
        private BookFile _firstFile;
        private BookFile _secondFile;

        [SetUp]
        public void Setup()
        {
            Mocker.Resolve<UpgradableSpecification>();

            CustomFormatsTestHelpers.GivenCustomFormats();

            _firstFile = new BookFile { Quality = new QualityModel(Quality.FLAC, new Revision(version: 2)), DateAdded = DateTime.Now };
            _secondFile = new BookFile { Quality = new QualityModel(Quality.FLAC, new Revision(version: 2)), DateAdded = DateTime.Now };

            var singleBookList = new List<Book> { new Book { BookFiles = new List<BookFile>() } };
            var doubleBookList = new List<Book> { new Book { BookFiles = new List<BookFile>() }, new Book { BookFiles = new List<BookFile>() }, new Book { BookFiles = new List<BookFile>() } };

            var fakeAuthor = Builder<Author>.CreateNew()
                         .With(c => c.QualityProfile = new QualityProfile
                         {
                             UpgradeAllowed = true,
                             Cutoff = Quality.MP3.Id,
                             Items = Qualities.QualityFixture.GetDefaultQualities(),
                             FormatItems = CustomFormatsTestHelpers.GetSampleFormatItems("None"),
                             MinFormatScore = 0,
                         })
                         .Build();

            Mocker.GetMock<IMediaFileService>()
                  .Setup(c => c.GetFilesByBook(It.IsAny<int>()))
                  .Returns(new List<BookFile> { _firstFile, _secondFile });

            _parseResultMulti = new RemoteBook
            {
                Author = fakeAuthor,
                ParsedBookInfo = new ParsedBookInfo { Quality = new QualityModel(Quality.MP3, new Revision(version: 2)) },
                Books = doubleBookList,
                CustomFormats = new List<CustomFormat>()
            };

            _parseResultSingle = new RemoteBook
            {
                Author = fakeAuthor,
                ParsedBookInfo = new ParsedBookInfo { Quality = new QualityModel(Quality.MP3, new Revision(version: 2)) },
                Books = singleBookList,
                CustomFormats = new List<CustomFormat>()
            };

            Mocker.GetMock<ICustomFormatCalculationService>()
                  .Setup(x => x.ParseCustomFormat(It.IsAny<BookFile>()))
                  .Returns(new List<CustomFormat>());
        }

        private void WithFirstFileUpgradable()
        {
            _firstFile.Quality = new QualityModel(Quality.MP3);
        }

        private void WithSecondFileUpgradable()
        {
            _secondFile.Quality = new QualityModel(Quality.MP3);
        }

        [Test]
        public void should_return_true_if_book_has_no_existing_file()
        {
            _parseResultSingle.Books.First().BookFiles = new List<BookFile>();

            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_return_true_if_track_is_missing()
        {
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_only_query_db_for_missing_tracks_once()
        {
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_return_true_if_single_book_doesnt_exist_on_disk()
        {
            _parseResultSingle.Books = new List<Book>();

            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_upgradable_if_all_files_are_upgradable()
        {
            WithFirstFileUpgradable();
            WithSecondFileUpgradable();
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_not_be_upgradable_if_qualities_are_the_same()
        {
            _firstFile.Quality = new QualityModel(Quality.MP3);
            _secondFile.Quality = new QualityModel(Quality.MP3);
            _parseResultSingle.ParsedBookInfo.Quality = new QualityModel(Quality.MP3);
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_not_be_upgradable_if_all_tracks_are_not_upgradable()
        {
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }

        [Test]
        public void should_be_true_if_some_tracks_are_upgradable_and_none_are_downgrades()
        {
            WithFirstFileUpgradable();
            _parseResultSingle.ParsedBookInfo.Quality = _secondFile.Quality;
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeTrue();
        }

        [Test]
        public void should_be_false_if_some_tracks_are_upgradable_and_some_are_downgrades()
        {
            Mocker.GetMock<ICustomFormatCalculationService>()
                  .Setup(s => s.ParseCustomFormat(It.IsAny<BookFile>()))
                  .Returns(new List<CustomFormat>());

            WithFirstFileUpgradable();
            _parseResultSingle.ParsedBookInfo.Quality = new QualityModel(Quality.MP3);
            Subject.IsSatisfiedBy(_parseResultSingle, null).Accepted.Should().BeFalse();
        }
    }
}
