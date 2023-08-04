using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.RootFolders;
using NzbDrone.Core.Test.Framework;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.RootFolderTests
{
    [TestFixture]
    public class GetBestRootFolderPathFixture : CoreTest<RootFolderService>
    {
        private void GivenRootFolders(params string[] paths)
        {
            Mocker.GetMock<IRootFolderRepository>()
                .Setup(s => s.All())
                .Returns(paths.Select(p => new RootFolder { Path = p }));
        }

        [Test]
        public void should_return_root_folder_that_is_parent_path()
        {
            GivenRootFolders(@"C:\Test\Books".AsOsAgnostic(), @"D:\Test\Books".AsOsAgnostic());
            Subject.GetBestRootFolderPath(@"C:\Test\Books\Author Title".AsOsAgnostic()).Should().Be(@"C:\Test\Books".AsOsAgnostic());
        }

        [Test]
        public void should_return_root_folder_that_is_grandparent_path()
        {
            GivenRootFolders(@"C:\Test\Books".AsOsAgnostic(), @"D:\Test\Books".AsOsAgnostic());
            Subject.GetBestRootFolderPath(@"C:\Test\Books\S\Author Title".AsOsAgnostic()).Should().Be(@"C:\Test\Books".AsOsAgnostic());
        }

        [Test]
        public void should_get_parent_path_from_os_path_if_matching_root_folder_is_not_found()
        {
            var artistPath = @"T:\Test\Books\Author Title".AsOsAgnostic();

            GivenRootFolders(@"C:\Test\Books".AsOsAgnostic(), @"D:\Test\Books".AsOsAgnostic());
            Subject.GetBestRootFolderPath(artistPath).Should().Be(@"T:\Test\Books".AsOsAgnostic());
        }

        [Test]
        public void should_get_parent_path_from_os_path_if_matching_root_folder_is_not_found_for_posix_path()
        {
            WindowsOnly();

            var artistPath = "/mnt/books/Author Title";

            GivenRootFolders(@"C:\Test\Books".AsOsAgnostic(), @"D:\Test\Books".AsOsAgnostic());
            Subject.GetBestRootFolderPath(artistPath).Should().Be(@"/mnt/books");
        }

        [Test]
        public void should_get_parent_path_from_os_path_if_matching_root_folder_is_not_found_for_windows_path()
        {
            PosixOnly();

            var artistPath = @"T:\Test\Books\Author Title";

            GivenRootFolders(@"C:\Test\Books".AsOsAgnostic(), @"D:\Test\Books".AsOsAgnostic());
            Subject.GetBestRootFolderPath(artistPath).Should().Be(@"T:\Test\Books");
        }
    }
}
