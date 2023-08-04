using System.Collections.Generic;
using System.Linq;
using FizzWare.NBuilder;
using Moq;
using NUnit.Framework;
using NzbDrone.Common.Disk;
using NzbDrone.Core.Books;
using NzbDrone.Core.HealthCheck.Checks;
using NzbDrone.Core.ImportLists;
using NzbDrone.Core.Localization;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.HealthCheck.Checks
{
    [TestFixture]
    public class RootFolderCheckFixture : CoreTest<RootFolderCheck>
    {
        [SetUp]
        public void Setup()
        {
            Mocker.GetMock<ILocalizationService>()
                  .Setup(s => s.GetLocalizedString(It.IsAny<string>()))
                  .Returns("Some Warning Message");
        }

        private void GivenMissingRootFolder(string rootFolderPath)
        {
            var author = Builder<Author>.CreateListOfSize(1)
                                        .Build()
                                        .ToList();

            var importList = Builder<ImportListDefinition>.CreateListOfSize(1)
                .Build()
                .ToList();

            Mocker.GetMock<IAuthorService>()
                  .Setup(s => s.AllAuthorPaths())
                  .Returns(author.ToDictionary(x => x.Id, x => x.Path));

            Mocker.GetMock<IImportListFactory>()
                .Setup(s => s.All())
                .Returns(importList);

            Mocker.GetMock<IRootFolderService>()
                  .Setup(s => s.GetBestRootFolderPath(It.IsAny<string>()))
                  .Returns(rootFolderPath);

            Mocker.GetMock<IDiskProvider>()
                  .Setup(s => s.FolderExists(It.IsAny<string>()))
                  .Returns(false);
        }

        [Test]
        public void should_not_return_error_when_no_book()
        {
            Mocker.GetMock<IAuthorService>()
                  .Setup(s => s.AllAuthorPaths())
                  .Returns(new Dictionary<int, string>());

            Mocker.GetMock<IImportListFactory>()
                .Setup(s => s.All())
                .Returns(new List<ImportListDefinition>());

            Subject.Check().ShouldBeOk();
        }

        [Test]
        public void should_return_error_if_book_parent_is_missing()
        {
            GivenMissingRootFolder(@"C:\Books".AsOsAgnostic());

            Subject.Check().ShouldBeError();
        }

        [Test]
        public void should_return_error_if_series_path_is_for_posix_os()
        {
            WindowsOnly();
            GivenMissingRootFolder("/mnt/books");

            Subject.Check().ShouldBeError();
        }

        [Test]
        public void should_return_error_if_series_path_is_for_windows()
        {
            PosixOnly();
            GivenMissingRootFolder(@"C:\Books");

            Subject.Check().ShouldBeError();
        }
    }
}
