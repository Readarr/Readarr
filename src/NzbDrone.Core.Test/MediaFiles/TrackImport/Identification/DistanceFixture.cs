using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.MediaFiles.BookImport.Identification;
using NzbDrone.Test.Common;

namespace NzbDrone.Core.Test.MediaFiles.BookImport.Identification
{
    [TestFixture]
    public class DistanceFixture : TestBase
    {
        [Test]
        public void test_add()
        {
            var dist = new Distance();
            dist.Add("add", 1.0);
            dist.Penalties.Should().BeEquivalentTo(new Dictionary<string, List<double>> { { "add", new List<double> { 1.0 } } });
        }

        [Test]
        public void test_equality()
        {
            var dist = new Distance();
            dist.AddEquality("equality", "ghi", new List<string> { "abc", "def", "ghi" });
            dist.Penalties.Should().BeEquivalentTo(new Dictionary<string, List<double>> { { "equality", new List<double> { 0.0 } } });

            dist.AddEquality("equality", "xyz", new List<string> { "abc", "def", "ghi" });
            dist.Penalties.Should().BeEquivalentTo(new Dictionary<string, List<double>> { { "equality", new List<double> { 0.0, 1.0 } } });

            dist.AddEquality("equality", "abc", new List<string> { "abc", "def", "ghi" });
            dist.Penalties.Should().BeEquivalentTo(new Dictionary<string, List<double>> { { "equality", new List<double> { 0.0, 1.0, 0.0 } } });
        }

        [Test]
        public void test_add_bool()
        {
            var dist = new Distance();
            dist.AddBool("expr", true);
            dist.Penalties.Should().BeEquivalentTo(new Dictionary<string, List<double>> { { "expr", new List<double> { 1.0 } } });

            dist.AddBool("expr", false);
            dist.Penalties.Should().BeEquivalentTo(new Dictionary<string, List<double>> { { "expr", new List<double> { 1.0, 0.0 } } });
        }

        [Test]
        public void test_add_number()
        {
            var dist = new Distance();
            dist.AddNumber("number", 1, 1);
            dist.Penalties.Should().BeEquivalentTo(new Dictionary<string, List<double>> { { "number", new List<double> { 0.0 } } });

            dist.AddNumber("number", 1, 2);
            dist.Penalties.Should().BeEquivalentTo(new Dictionary<string, List<double>> { { "number", new List<double> { 0.0, 1.0 } } });

            dist.AddNumber("number", 2, 1);
            dist.Penalties.Should().BeEquivalentTo(new Dictionary<string, List<double>> { { "number", new List<double> { 0.0, 1.0, 1.0 } } });

            dist.AddNumber("number", -1, 2);
            dist.Penalties.Should().BeEquivalentTo(new Dictionary<string, List<double>> { { "number", new List<double> { 0.0, 1.0, 1.0, 1.0, 1.0, 1.0 } } });
        }

        [Test]
        public void test_add_priority_value()
        {
            var dist = new Distance();
            dist.AddPriority("priority", "abc", new List<string> { "abc" });
            dist.Penalties.Should().BeEquivalentTo(new Dictionary<string, List<double>> { { "priority", new List<double> { 0.0 } } });

            dist.AddPriority("priority", "def", new List<string> { "abc", "def" });
            dist.Penalties.Should().BeEquivalentTo(new Dictionary<string, List<double>> { { "priority", new List<double> { 0.0, 0.5 } } });

            dist.AddPriority("priority", "xyz", new List<string> { "abc", "def" });
            dist.Penalties.Should().BeEquivalentTo(new Dictionary<string, List<double>> { { "priority", new List<double> { 0.0, 0.5, 1.0 } } });
        }

        [Test]
        public void test_add_priority_list()
        {
            var dist = new Distance();
            dist.AddPriority("priority", new List<string> { "abc" }, new List<string> { "abc" });
            dist.Penalties.Should().BeEquivalentTo(new Dictionary<string, List<double>> { { "priority", new List<double> { 0.0 } } });

            dist.AddPriority("priority", new List<string> { "def" }, new List<string> { "abc" });
            dist.Penalties.Should().BeEquivalentTo(new Dictionary<string, List<double>> { { "priority", new List<double> { 0.0, 1.0 } } });

            dist.AddPriority("priority", new List<string> { "abc", "xyz" }, new List<string> { "abc" });
            dist.Penalties.Should().BeEquivalentTo(new Dictionary<string, List<double>> { { "priority", new List<double> { 0.0, 1.0, 0.0 } } });

            dist.AddPriority("priority", new List<string> { "def", "xyz" }, new List<string> { "abc", "def" });
            dist.Penalties.Should().BeEquivalentTo(new Dictionary<string, List<double>> { { "priority", new List<double> { 0.0, 1.0, 0.0, 0.5 } } });
        }

        [Test]
        public void test_add_ratio()
        {
            var dist = new Distance();
            dist.AddRatio("ratio", 25, 100);
            dist.Penalties.Should().BeEquivalentTo(new Dictionary<string, List<double>> { { "ratio", new List<double> { 0.25 } } });

            dist.AddRatio("ratio", 10, 5);
            dist.Penalties.Should().BeEquivalentTo(new Dictionary<string, List<double>> { { "ratio", new List<double> { 0.25, 1.0 } } });

            dist.AddRatio("ratio", -5, 5);
            dist.Penalties.Should().BeEquivalentTo(new Dictionary<string, List<double>> { { "ratio", new List<double> { 0.25, 1.0, 0.0 } } });

            dist.AddRatio("ratio", 5, 0);
            dist.Penalties.Should().BeEquivalentTo(new Dictionary<string, List<double>> { { "ratio", new List<double> { 0.25, 1.0, 0.0, 0.0 } } });
        }

        [Test]
        public void test_add_string()
        {
            var dist = new Distance();
            dist.AddString("string", "abcd", "bcde");
            dist.Penalties.Should().BeEquivalentTo(new Dictionary<string, List<double>> { { "string", new List<double> { 0.5 } } });
        }

        [Test]
        public void test_add_string_none()
        {
            var dist = new Distance();
            dist.AddString("string", string.Empty, "bcd");
            dist.Penalties.Should().BeEquivalentTo(new Dictionary<string, List<double>> { { "string", new List<double> { 1.0 } } });
        }

        [Test]
        public void test_add_string_both_none()
        {
            var dist = new Distance();
            dist.AddString("string", string.Empty, string.Empty);
            dist.Penalties.Should().BeEquivalentTo(new Dictionary<string, List<double>> { { "string", new List<double> { 0.0 } } });
        }

        [Test]
        public void test_add_string_empty_values_valid_target()
        {
            var dist = new Distance();
            dist.AddString("string", new List<string>(), "target");
            dist.Penalties.Should().BeEquivalentTo(new Dictionary<string, List<double>> { { "string", new List<double> { 1.0 } } });
        }

        [Test]
        public void test_add_string_empty_values_empty_target()
        {
            var dist = new Distance();
            dist.AddString("string", new List<string>(), string.Empty);
            dist.Penalties.Should().BeEquivalentTo(new Dictionary<string, List<double>> { { "string", new List<double> { 0.0 } } });
        }

        [Test]
        public void test_add_string_empty_options_valid_value()
        {
            var dist = new Distance();
            dist.AddString("string", "value", new List<string>());
            dist.Penalties.Should().BeEquivalentTo(new Dictionary<string, List<double>> { { "string", new List<double> { 1.0 } } });
        }

        [Test]
        public void test_add_string_empty_options_empty_value()
        {
            var dist = new Distance();
            dist.AddString("string", string.Empty, new List<string>());
            dist.Penalties.Should().BeEquivalentTo(new Dictionary<string, List<double>> { { "string", new List<double> { 0.0 } } });
        }

        [Test]
        public void test_add_string_multiple_options_multiple_values_match()
        {
            var dist = new Distance();
            dist.AddString("string", new List<string> { "cat", "dog" }, new List<string> { "dog", "mouse" });
            dist.Penalties.Should().BeEquivalentTo(new Dictionary<string, List<double>> { { "string", new List<double> { 0.0 } } });
        }

        [Test]
        public void test_add_string_multiple_options_multiple_values_no_match()
        {
            var dist = new Distance();
            dist.AddString("string", new List<string> { "cat", "dog" }, new List<string> { "y", "z" });
            dist.Penalties.Should().BeEquivalentTo(new Dictionary<string, List<double>> { { "string", new List<double> { 1.0 } } });
        }

        [Test]
        public void test_distance()
        {
            var dist = new Distance();
            dist.Add("book", 0.5);
            dist.Add("media_count", 0.25);
            dist.Add("media_count", 0.75);

            dist.NormalizedDistance().Should().Be(0.5);
        }

        [Test]
        public void test_max_distance()
        {
            var dist = new Distance();
            dist.Add("book", 0.5);
            dist.Add("media_count", 0.0);
            dist.Add("media_count", 0.0);

            dist.MaxDistance().Should().Be(5.0);
        }

        [Test]
        public void test_raw_distance()
        {
            var dist = new Distance();
            dist.Add("book", 0.5);
            dist.Add("media_count", 0.25);
            dist.Add("media_count", 0.5);

            dist.RawDistance().Should().Be(2.25);
        }
    }
}
