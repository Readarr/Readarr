using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using FluentValidation.Results;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.Books.Commands;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.ImportLists.Exclusions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.BookImport;
using NzbDrone.Core.MediaFiles.BookImport.Aggregation;
using NzbDrone.Core.MediaFiles.BookImport.Aggregation.Aggregators;
using NzbDrone.Core.MediaFiles.BookImport.Identification;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.MetadataSource.BookInfo;
using NzbDrone.Core.MetadataSource.Goodreads;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Profiles.Metadata;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.BookImport.Identification
{
    [TestFixture]
    public class IdentificationServiceFixture : DbTest
    {
        private AuthorService _authorService;
        private AddAuthorService _addAuthorService;
        private RefreshAuthorService _refreshAuthorService;

        private IdentificationService _Subject;

        [SetUp]
        public void SetUp()
        {
            UseRealHttp();

            // Resolve all the parts we need
            Mocker.SetConstant<IAuthorRepository>(Mocker.Resolve<AuthorRepository>());
            Mocker.SetConstant<IAuthorMetadataRepository>(Mocker.Resolve<AuthorMetadataRepository>());
            Mocker.SetConstant<IBookRepository>(Mocker.Resolve<BookRepository>());
            Mocker.SetConstant<IImportListExclusionRepository>(Mocker.Resolve<ImportListExclusionRepository>());
            Mocker.SetConstant<IMediaFileRepository>(Mocker.Resolve<MediaFileRepository>());

            Mocker.GetMock<IMetadataProfileService>().Setup(x => x.Exists(It.IsAny<int>())).Returns(true);

            _authorService = Mocker.Resolve<AuthorService>();
            Mocker.SetConstant<IAuthorService>(_authorService);
            Mocker.SetConstant<IAuthorMetadataService>(Mocker.Resolve<AuthorMetadataService>());
            Mocker.SetConstant<IBookService>(Mocker.Resolve<BookService>());
            Mocker.SetConstant<IImportListExclusionService>(Mocker.Resolve<ImportListExclusionService>());
            Mocker.SetConstant<IMediaFileService>(Mocker.Resolve<MediaFileService>());

            Mocker.SetConstant<IConfigService>(Mocker.Resolve<IConfigService>());
            Mocker.SetConstant<IProvideAuthorInfo>(Mocker.Resolve<BookInfoProxy>());
            Mocker.SetConstant<IProvideBookInfo>(Mocker.Resolve<BookInfoProxy>());

            _addAuthorService = Mocker.Resolve<AddAuthorService>();

            Mocker.SetConstant<IRefreshBookService>(Mocker.Resolve<RefreshBookService>());
            _refreshAuthorService = Mocker.Resolve<RefreshAuthorService>();

            Mocker.GetMock<IAddAuthorValidator>().Setup(x => x.Validate(It.IsAny<Author>())).Returns(new ValidationResult());

            Mocker.SetConstant<ITrackGroupingService>(Mocker.Resolve<TrackGroupingService>());
            Mocker.SetConstant<ICandidateService>(Mocker.Resolve<CandidateService>());

            // set up the augmenters
            List<IAggregate<LocalEdition>> aggregators = new List<IAggregate<LocalEdition>>
            {
                Mocker.Resolve<AggregateFilenameInfo>()
            };
            Mocker.SetConstant<IEnumerable<IAggregate<LocalEdition>>>(aggregators);
            Mocker.SetConstant<IAugmentingService>(Mocker.Resolve<AugmentingService>());

            _Subject = Mocker.Resolve<IdentificationService>();
        }

        private void GivenMetadataProfile(MetadataProfile profile)
        {
            Mocker.GetMock<IMetadataProfileService>().Setup(x => x.Get(profile.Id)).Returns(profile);
        }

        private List<Author> GivenAuthors(List<AuthorTestCase> authors)
        {
            var outp = new List<Author>();
            for (int i = 0; i < authors.Count; i++)
            {
                var meta = authors[i].MetadataProfile;
                meta.Id = i + 1;
                GivenMetadataProfile(meta);
                outp.Add(GivenAuthor(authors[i].Author, meta.Id));
            }

            return outp;
        }

        private Author GivenAuthor(string foreignAuthorId, int metadataProfileId)
        {
            var author = _addAuthorService.AddAuthor(new Author
            {
                Metadata = new AuthorMetadata
                {
                    ForeignAuthorId = foreignAuthorId
                },
                Path = @"c:\test".AsOsAgnostic(),
                MetadataProfileId = metadataProfileId
            });

            var command = new RefreshAuthorCommand
            {
                AuthorId = author.Id,
                Trigger = CommandTrigger.Unspecified
            };

            _refreshAuthorService.Execute(command);

            return _authorService.FindById(foreignAuthorId);
        }

        public static class IdTestCaseFactory
        {
            // for some reason using Directory.GetFiles causes nUnit to error
            private static string[] files =
            {
                "FilesWithMBIds.json",
                "PreferMissingToBadMatch.json",
                "InconsistentTyposInBook.json",
                "SucceedWhenManyBooksHaveSameTitle.json",
                "PenalizeUnknownMedia.json",
                "CorruptFile.json",
                "FilesWithoutTags.json"
            };

            public static IEnumerable TestCases
            {
                get
                {
                    foreach (var file in files)
                    {
                        yield return new TestCaseData(file).SetName($"should_match_tracks_{file.Replace(".json", "")}");
                    }
                }
            }
        }

        // these are slow to run so only do so manually
        [Explicit]
        [TestCaseSource(typeof(IdTestCaseFactory), "TestCases")]
        public void should_match_tracks(string file)
        {
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Files", "Identification", file);
            var testcase = JsonConvert.DeserializeObject<IdTestCase>(File.ReadAllText(path));

            var authors = GivenAuthors(testcase.LibraryAuthors);
            var specifiedAuthor = authors.SingleOrDefault(x => x.Metadata.Value.ForeignAuthorId == testcase.Author);
            var idOverrides = new IdentificationOverrides { Author = specifiedAuthor };

            var tracks = testcase.Tracks.Select(x => new LocalBook
            {
                Path = x.Path.AsOsAgnostic(),
                FileTrackInfo = x.FileTrackInfo
            }).ToList();

            var config = new ImportDecisionMakerConfig
            {
                NewDownload = testcase.NewDownload,
                SingleRelease = testcase.SingleRelease,
                IncludeExisting = false
            };

            var result = _Subject.Identify(tracks, idOverrides, config);

            result.Should().HaveCount(testcase.ExpectedMusicBrainzReleaseIds.Count);
        }
    }
}
