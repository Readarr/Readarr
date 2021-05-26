using System.Collections.Generic;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.DecisionEngine;
using NzbDrone.Core.Download;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.BookImport;
using NzbDrone.Core.MediaFiles.BookImport.Aggregation;
using NzbDrone.Core.MediaFiles.BookImport.Identification;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.BookImport
{
    [TestFixture]
    public class ImportDecisionMakerFixture : FileSystemTest<ImportDecisionMaker>
    {
        private List<IFileInfo> _fileInfos;
        private LocalBook _localTrack;
        private Author _author;
        private Book _book;
        private Edition _edition;
        private QualityModel _quality;

        private IdentificationOverrides _idOverrides;
        private ImportDecisionMakerConfig _idConfig;

        private Mock<IImportDecisionEngineSpecification<LocalEdition>> _bookpass1;
        private Mock<IImportDecisionEngineSpecification<LocalEdition>> _bookpass2;
        private Mock<IImportDecisionEngineSpecification<LocalEdition>> _bookpass3;

        private Mock<IImportDecisionEngineSpecification<LocalEdition>> _bookfail1;
        private Mock<IImportDecisionEngineSpecification<LocalEdition>> _bookfail2;
        private Mock<IImportDecisionEngineSpecification<LocalEdition>> _bookfail3;

        private Mock<IImportDecisionEngineSpecification<LocalBook>> _pass1;
        private Mock<IImportDecisionEngineSpecification<LocalBook>> _pass2;
        private Mock<IImportDecisionEngineSpecification<LocalBook>> _pass3;

        private Mock<IImportDecisionEngineSpecification<LocalBook>> _fail1;
        private Mock<IImportDecisionEngineSpecification<LocalBook>> _fail2;
        private Mock<IImportDecisionEngineSpecification<LocalBook>> _fail3;

        [SetUp]
        public void Setup()
        {
            _bookpass1 = new Mock<IImportDecisionEngineSpecification<LocalEdition>>();
            _bookpass2 = new Mock<IImportDecisionEngineSpecification<LocalEdition>>();
            _bookpass3 = new Mock<IImportDecisionEngineSpecification<LocalEdition>>();

            _bookfail1 = new Mock<IImportDecisionEngineSpecification<LocalEdition>>();
            _bookfail2 = new Mock<IImportDecisionEngineSpecification<LocalEdition>>();
            _bookfail3 = new Mock<IImportDecisionEngineSpecification<LocalEdition>>();

            _pass1 = new Mock<IImportDecisionEngineSpecification<LocalBook>>();
            _pass2 = new Mock<IImportDecisionEngineSpecification<LocalBook>>();
            _pass3 = new Mock<IImportDecisionEngineSpecification<LocalBook>>();

            _fail1 = new Mock<IImportDecisionEngineSpecification<LocalBook>>();
            _fail2 = new Mock<IImportDecisionEngineSpecification<LocalBook>>();
            _fail3 = new Mock<IImportDecisionEngineSpecification<LocalBook>>();

            _bookpass1.Setup(c => c.IsSatisfiedBy(It.IsAny<LocalEdition>(), It.IsAny<DownloadClientItem>())).Returns(Decision.Accept());
            _bookpass2.Setup(c => c.IsSatisfiedBy(It.IsAny<LocalEdition>(), It.IsAny<DownloadClientItem>())).Returns(Decision.Accept());
            _bookpass3.Setup(c => c.IsSatisfiedBy(It.IsAny<LocalEdition>(), It.IsAny<DownloadClientItem>())).Returns(Decision.Accept());

            _bookfail1.Setup(c => c.IsSatisfiedBy(It.IsAny<LocalEdition>(), It.IsAny<DownloadClientItem>())).Returns(Decision.Reject("_bookfail1"));
            _bookfail2.Setup(c => c.IsSatisfiedBy(It.IsAny<LocalEdition>(), It.IsAny<DownloadClientItem>())).Returns(Decision.Reject("_bookfail2"));
            _bookfail3.Setup(c => c.IsSatisfiedBy(It.IsAny<LocalEdition>(), It.IsAny<DownloadClientItem>())).Returns(Decision.Reject("_bookfail3"));

            _pass1.Setup(c => c.IsSatisfiedBy(It.IsAny<LocalBook>(), It.IsAny<DownloadClientItem>())).Returns(Decision.Accept());
            _pass2.Setup(c => c.IsSatisfiedBy(It.IsAny<LocalBook>(), It.IsAny<DownloadClientItem>())).Returns(Decision.Accept());
            _pass3.Setup(c => c.IsSatisfiedBy(It.IsAny<LocalBook>(), It.IsAny<DownloadClientItem>())).Returns(Decision.Accept());

            _fail1.Setup(c => c.IsSatisfiedBy(It.IsAny<LocalBook>(), It.IsAny<DownloadClientItem>())).Returns(Decision.Reject("_fail1"));
            _fail2.Setup(c => c.IsSatisfiedBy(It.IsAny<LocalBook>(), It.IsAny<DownloadClientItem>())).Returns(Decision.Reject("_fail2"));
            _fail3.Setup(c => c.IsSatisfiedBy(It.IsAny<LocalBook>(), It.IsAny<DownloadClientItem>())).Returns(Decision.Reject("_fail3"));

            _author = Builder<Author>.CreateNew()
                .With(e => e.QualityProfileId = 1)
                .With(e => e.QualityProfile = new QualityProfile { Items = Qualities.QualityFixture.GetDefaultQualities() })
                .Build();

            _book = Builder<Book>.CreateNew()
                .With(x => x.Author = _author)
                .Build();

            _edition = Builder<Edition>.CreateNew()
                .With(x => x.Book = _book)
                .Build();

            _quality = new QualityModel(Quality.MP3_320);

            _localTrack = new LocalBook
            {
                Author = _author,
                Quality = _quality,
                Book = new Book(),
                Path = @"C:\Test\Unsorted\The.Office.S03E115.DVDRip.XviD-OSiTV.avi".AsOsAgnostic()
            };

            _idOverrides = new IdentificationOverrides
            {
                Author = _author
            };

            _idConfig = new ImportDecisionMakerConfig();

            GivenAudioFiles(new List<string> { @"C:\Test\Unsorted\The.Office.S03E115.DVDRip.XviD-OSiTV.avi".AsOsAgnostic() });

            Mocker.GetMock<IIdentificationService>()
                .Setup(s => s.Identify(It.IsAny<List<LocalBook>>(), It.IsAny<IdentificationOverrides>(), It.IsAny<ImportDecisionMakerConfig>()))
                .Returns((List<LocalBook> tracks, IdentificationOverrides idOverrides, ImportDecisionMakerConfig config) =>
                {
                    var ret = new LocalEdition(tracks);
                    ret.Edition = _edition;
                    return new List<LocalEdition> { ret };
                });

            Mocker.GetMock<IMediaFileService>()
                .Setup(c => c.FilterUnchangedFiles(It.IsAny<List<IFileInfo>>(), It.IsAny<FilterFilesType>()))
                .Returns((List<IFileInfo> files, FilterFilesType filter) => files);

            Mocker.GetMock<IMetadataTagService>()
                .Setup(s => s.ReadTags(It.IsAny<IFileInfo>()))
                .Returns(new ParsedTrackInfo());

            GivenSpecifications(_bookpass1);
        }

        private void GivenSpecifications<T>(params Mock<IImportDecisionEngineSpecification<T>>[] mocks)
        {
            Mocker.SetConstant(mocks.Select(c => c.Object));
        }

        private void GivenAudioFiles(IEnumerable<string> videoFiles)
        {
            foreach (var file in videoFiles)
            {
                FileSystem.AddFile(file, new MockFileData(string.Empty));
            }

            _fileInfos = videoFiles.Select(x => DiskProvider.GetFileInfo(x)).ToList();
        }

        private void GivenAugmentationSuccess()
        {
            Mocker.GetMock<IAugmentingService>()
                  .Setup(s => s.Augment(It.IsAny<LocalBook>(), It.IsAny<bool>()))
                  .Callback<LocalBook, bool>((localTrack, otherFiles) =>
                  {
                      localTrack.Book = _localTrack.Book;
                  });
        }

        [Test]
        public void should_call_all_book_specifications()
        {
            var downloadClientItem = Builder<DownloadClientItem>.CreateNew().Build();
            var itemInfo = new ImportDecisionMakerInfo { DownloadClientItem = downloadClientItem };

            GivenAugmentationSuccess();
            GivenSpecifications(_bookpass1, _bookpass2, _bookpass3, _bookfail1, _bookfail2, _bookfail3);

            Subject.GetImportDecisions(_fileInfos, null, itemInfo, _idConfig);

            _bookfail1.Verify(c => c.IsSatisfiedBy(It.IsAny<LocalEdition>(), It.IsAny<DownloadClientItem>()), Times.Once());
            _bookfail2.Verify(c => c.IsSatisfiedBy(It.IsAny<LocalEdition>(), It.IsAny<DownloadClientItem>()), Times.Once());
            _bookfail3.Verify(c => c.IsSatisfiedBy(It.IsAny<LocalEdition>(), It.IsAny<DownloadClientItem>()), Times.Once());
            _bookpass1.Verify(c => c.IsSatisfiedBy(It.IsAny<LocalEdition>(), It.IsAny<DownloadClientItem>()), Times.Once());
            _bookpass2.Verify(c => c.IsSatisfiedBy(It.IsAny<LocalEdition>(), It.IsAny<DownloadClientItem>()), Times.Once());
            _bookpass3.Verify(c => c.IsSatisfiedBy(It.IsAny<LocalEdition>(), It.IsAny<DownloadClientItem>()), Times.Once());
        }

        [Test]
        public void should_call_all_track_specifications_if_book_accepted()
        {
            var downloadClientItem = Builder<DownloadClientItem>.CreateNew().Build();
            var itemInfo = new ImportDecisionMakerInfo { DownloadClientItem = downloadClientItem };

            GivenAugmentationSuccess();
            GivenSpecifications(_pass1, _pass2, _pass3, _fail1, _fail2, _fail3);

            Subject.GetImportDecisions(_fileInfos, null, itemInfo, _idConfig);

            _fail1.Verify(c => c.IsSatisfiedBy(It.IsAny<LocalBook>(), It.IsAny<DownloadClientItem>()), Times.Once());
            _fail2.Verify(c => c.IsSatisfiedBy(It.IsAny<LocalBook>(), It.IsAny<DownloadClientItem>()), Times.Once());
            _fail3.Verify(c => c.IsSatisfiedBy(It.IsAny<LocalBook>(), It.IsAny<DownloadClientItem>()), Times.Once());
            _pass1.Verify(c => c.IsSatisfiedBy(It.IsAny<LocalBook>(), It.IsAny<DownloadClientItem>()), Times.Once());
            _pass2.Verify(c => c.IsSatisfiedBy(It.IsAny<LocalBook>(), It.IsAny<DownloadClientItem>()), Times.Once());
            _pass3.Verify(c => c.IsSatisfiedBy(It.IsAny<LocalBook>(), It.IsAny<DownloadClientItem>()), Times.Once());
        }

        [Test]
        public void should_call_no_track_specifications_if_book_rejected()
        {
            var downloadClientItem = Builder<DownloadClientItem>.CreateNew().Build();
            var itemInfo = new ImportDecisionMakerInfo { DownloadClientItem = downloadClientItem };

            GivenAugmentationSuccess();
            GivenSpecifications(_bookpass1, _bookpass2, _bookpass3, _bookfail1, _bookfail2, _bookfail3);
            GivenSpecifications(_pass1, _pass2, _pass3, _fail1, _fail2, _fail3);

            Subject.GetImportDecisions(_fileInfos, null, itemInfo, _idConfig);

            _fail1.Verify(c => c.IsSatisfiedBy(It.IsAny<LocalBook>(), It.IsAny<DownloadClientItem>()), Times.Never());
            _fail2.Verify(c => c.IsSatisfiedBy(It.IsAny<LocalBook>(), It.IsAny<DownloadClientItem>()), Times.Never());
            _fail3.Verify(c => c.IsSatisfiedBy(It.IsAny<LocalBook>(), It.IsAny<DownloadClientItem>()), Times.Never());
            _pass1.Verify(c => c.IsSatisfiedBy(It.IsAny<LocalBook>(), It.IsAny<DownloadClientItem>()), Times.Never());
            _pass2.Verify(c => c.IsSatisfiedBy(It.IsAny<LocalBook>(), It.IsAny<DownloadClientItem>()), Times.Never());
            _pass3.Verify(c => c.IsSatisfiedBy(It.IsAny<LocalBook>(), It.IsAny<DownloadClientItem>()), Times.Never());
        }

        [Test]
        public void should_return_rejected_if_only_book_spec_fails()
        {
            GivenSpecifications(_bookfail1);
            GivenSpecifications(_pass1);

            var result = Subject.GetImportDecisions(_fileInfos, null, null, _idConfig);

            result.Single().Approved.Should().BeFalse();
        }

        [Test]
        public void should_return_rejected_if_only_track_spec_fails()
        {
            GivenSpecifications(_bookpass1);
            GivenSpecifications(_fail1);

            var result = Subject.GetImportDecisions(_fileInfos, null, null, _idConfig);

            result.Single().Approved.Should().BeFalse();
        }

        [Test]
        public void should_return_rejected_if_one_book_spec_fails()
        {
            GivenSpecifications(_bookpass1, _bookfail1, _bookpass2, _bookpass3);
            GivenSpecifications(_pass1, _pass2, _pass3);

            var result = Subject.GetImportDecisions(_fileInfos, null, null, _idConfig);

            result.Single().Approved.Should().BeFalse();
        }

        [Test]
        public void should_return_rejected_if_one_track_spec_fails()
        {
            GivenSpecifications(_bookpass1, _bookpass2, _bookpass3);
            GivenSpecifications(_pass1, _fail1, _pass2, _pass3);

            var result = Subject.GetImportDecisions(_fileInfos, null, null, _idConfig);

            result.Single().Approved.Should().BeFalse();
        }

        [Test]
        public void should_return_approved_if_all_specs_pass()
        {
            GivenAugmentationSuccess();
            GivenSpecifications(_bookpass1, _bookpass2, _bookpass3);
            GivenSpecifications(_pass1, _pass2, _pass3);

            var result = Subject.GetImportDecisions(_fileInfos, null, null, _idConfig);

            result.Single().Approved.Should().BeTrue();
        }

        [Test]
        public void should_have_same_number_of_rejections_as_specs_that_failed()
        {
            GivenAugmentationSuccess();
            GivenSpecifications(_pass1, _pass2, _pass3, _fail1, _fail2, _fail3);

            var result = Subject.GetImportDecisions(_fileInfos, null, null, _idConfig);
            result.Single().Rejections.Should().HaveCount(3);
        }

        [Test]
        public void should_not_blowup_the_process_due_to_failed_augment()
        {
            GivenSpecifications(_pass1);

            Mocker.GetMock<IAugmentingService>()
                  .Setup(c => c.Augment(It.IsAny<LocalBook>(), It.IsAny<bool>()))
                  .Throws<TestException>();

            GivenAudioFiles(new[]
                {
                    @"C:\Test\Unsorted\The.Office.S03E115.DVDRip.XviD-OSiTV".AsOsAgnostic(),
                    @"C:\Test\Unsorted\The.Office.S03E115.DVDRip.XviD-OSiTV".AsOsAgnostic(),
                    @"C:\Test\Unsorted\The.Office.S03E115.DVDRip.XviD-OSiTV".AsOsAgnostic()
                });

            var decisions = Subject.GetImportDecisions(_fileInfos, _idOverrides, null, _idConfig);

            Mocker.GetMock<IAugmentingService>()
                  .Verify(c => c.Augment(It.IsAny<LocalBook>(), It.IsAny<bool>()), Times.Exactly(_fileInfos.Count));

            ExceptionVerification.ExpectedErrors(3);
        }

        [Test]
        public void should_not_throw_if_release_not_identified()
        {
            GivenSpecifications(_pass1);

            GivenAudioFiles(new[]
                {
                    @"C:\Test\Unsorted\The.Office.S03E115.DVDRip.XviD-OSiTV".AsOsAgnostic(),
                    @"C:\Test\Unsorted\The.Office.S03E115.DVDRip.XviD-OSiTV".AsOsAgnostic(),
                    @"C:\Test\Unsorted\The.Office.S03E115.DVDRip.XviD-OSiTV".AsOsAgnostic()
                });

            Mocker.GetMock<IIdentificationService>()
                .Setup(s => s.Identify(It.IsAny<List<LocalBook>>(), It.IsAny<IdentificationOverrides>(), It.IsAny<ImportDecisionMakerConfig>()))
                .Returns((List<LocalBook> tracks, IdentificationOverrides idOverrides, ImportDecisionMakerConfig config) =>
                    {
                        return new List<LocalEdition> { new LocalEdition(tracks) };
                    });

            var decisions = Subject.GetImportDecisions(_fileInfos, _idOverrides, null, _idConfig);

            Mocker.GetMock<IAugmentingService>()
                  .Verify(c => c.Augment(It.IsAny<LocalBook>(), It.IsAny<bool>()), Times.Exactly(_fileInfos.Count));

            decisions.Should().HaveCount(3);
            decisions.First().Rejections.Should().NotBeEmpty();
        }

        [Test]
        public void should_not_throw_if_tracks_are_not_found()
        {
            GivenSpecifications(_pass1);

            GivenAudioFiles(new[]
                {
                    @"C:\Test\Unsorted\The.Office.S03E115.DVDRip.XviD-OSiTV".AsOsAgnostic(),
                    @"C:\Test\Unsorted\The.Office.S03E115.DVDRip.XviD-OSiTV".AsOsAgnostic(),
                    @"C:\Test\Unsorted\The.Office.S03E115.DVDRip.XviD-OSiTV".AsOsAgnostic()
                });

            var decisions = Subject.GetImportDecisions(_fileInfos, _idOverrides, null, _idConfig);

            Mocker.GetMock<IAugmentingService>()
                  .Verify(c => c.Augment(It.IsAny<LocalBook>(), It.IsAny<bool>()), Times.Exactly(_fileInfos.Count));

            decisions.Should().HaveCount(3);
            decisions.First().Rejections.Should().NotBeEmpty();
        }

        [Test]
        public void should_return_a_decision_when_exception_is_caught()
        {
            Mocker.GetMock<IAugmentingService>()
                  .Setup(c => c.Augment(It.IsAny<LocalBook>(), It.IsAny<bool>()))
                  .Throws<TestException>();

            GivenAudioFiles(new[]
                {
                    @"C:\Test\Unsorted\The.Office.S03E115.DVDRip.XviD-OSiTV".AsOsAgnostic()
                });

            Subject.GetImportDecisions(_fileInfos, _idOverrides, null, _idConfig).Should().HaveCount(1);

            ExceptionVerification.ExpectedErrors(1);
        }
    }
}
