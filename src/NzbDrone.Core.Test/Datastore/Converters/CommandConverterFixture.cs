using System.Data.SQLite;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Books.Commands;
using NzbDrone.Core.Datastore.Converters;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore.Converters
{
    [TestFixture]
    public class CommandConverterFixture : CoreTest<CommandConverter>
    {
        private SQLiteParameter _param;

        [SetUp]
        public void Setup()
        {
            _param = new SQLiteParameter();
        }

        [Test]
        public void should_return_json_string_when_saving_boolean_to_db()
        {
            var command = new RefreshAuthorCommand();

            Subject.SetValue(_param, command);
            _param.Value.Should().BeOfType<string>();
        }

        [Test]
        public void should_return_null_for_null_value_when_saving_to_db()
        {
            Subject.SetValue(_param, null);
            _param.Value.Should().BeNull();
        }

        [Test]
        public void should_return_command_when_getting_json_from_db()
        {
            var data = "{\"name\": \"RefreshAuthor\"}";

            Subject.Parse(data).Should().BeOfType<RefreshAuthorCommand>();
        }

        [Test]
        public void should_return_null_for_null_value_when_getting_from_db()
        {
            Subject.Parse(null).Should().BeNull();
        }
    }
}
