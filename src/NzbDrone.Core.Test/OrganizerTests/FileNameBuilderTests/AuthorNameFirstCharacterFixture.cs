using System.IO;
using System.Linq;
using FizzWare.NBuilder;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.Organizer;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.OrganizerTests.FileNameBuilderTests
{
    [TestFixture]
    public class AuthorNameFirstCharacterFixture : CoreTest<FileNameBuilder>
    {
        private Author _author;
        private NamingConfig _namingConfig;

        [SetUp]
        public void Setup()
        {
            _author = Builder<Author>
                    .CreateNew()
                    .Build();

            _namingConfig = NamingConfig.Default;
            _namingConfig.RenameBooks = true;

            Mocker.GetMock<INamingConfigService>()
                  .Setup(c => c.GetConfig()).Returns(_namingConfig);

            Mocker.GetMock<IQualityDefinitionService>()
                .Setup(v => v.Get(Moq.It.IsAny<Quality>()))
                .Returns<Quality>(v => Quality.DefaultQualityDefinitions.First(c => c.Quality == v));
        }

        [TestCase("The Mist", "M", "The Mist")]
        [TestCase("A", "A", "A")]
        [TestCase("30 Rock", "3", "30 Rock")]
        public void should_get_expected_folder_name_back(string title, string parent, string child)
        {
            _author.Name = title;
            _namingConfig.AuthorFolderFormat = "{Author NameFirstCharacter}\\{Author Name}";

            Subject.GetAuthorFolder(_author).Should().Be(Path.Combine(parent, child));
        }

        [Test]
        public void should_be_able_to_use_lower_case_first_character()
        {
            _author.Name = "Westworld";
            _namingConfig.AuthorFolderFormat = "{author namefirstcharacter}\\{author name}";

            Subject.GetAuthorFolder(_author).Should().Be(Path.Combine("w", "westworld"));
        }
    }
}
