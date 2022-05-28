using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Books;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Datastore
{
    [TestFixture]
    public class WhereBuilderPostgresFixture : CoreTest
    {
        private WhereBuilderPostgres _subject;

        [OneTimeSetUp]
        public void MapTables()
        {
            // Generate table mapping
            Mocker.Resolve<DbFactory>();
        }

        private WhereBuilderPostgres Where(Expression<Func<Author, bool>> filter)
        {
            return new WhereBuilderPostgres(filter, true, 0);
        }

        private WhereBuilderPostgres WhereMetadata(Expression<Func<AuthorMetadata, bool>> filter)
        {
            return new WhereBuilderPostgres(filter, true, 0);
        }

        [Test]
        public void postgres_where_equal_const()
        {
            _subject = Where(x => x.Id == 10);

            _subject.ToString().Should().Be($"(\"Authors\".\"Id\" = @Clause1_P1)");
            _subject.Parameters.Get<int>("Clause1_P1").Should().Be(10);
        }

        [Test]
        public void postgres_where_equal_variable()
        {
            var id = 10;
            _subject = Where(x => x.Id == id);

            _subject.ToString().Should().Be($"(\"Authors\".\"Id\" = @Clause1_P1)");
            _subject.Parameters.Get<int>("Clause1_P1").Should().Be(id);
        }

        [Test]
        public void postgres_where_equal_property()
        {
            var author = new Author { Id = 10 };
            _subject = Where(x => x.Id == author.Id);

            _subject.Parameters.ParameterNames.Should().HaveCount(1);
            _subject.ToString().Should().Be($"(\"Authors\".\"Id\" = @Clause1_P1)");
            _subject.Parameters.Get<int>("Clause1_P1").Should().Be(author.Id);
        }

        [Test]
        public void postgres_where_equal_joined_property()
        {
            _subject = Where(x => x.QualityProfile.Value.Id == 1);

            _subject.Parameters.ParameterNames.Should().HaveCount(1);
            _subject.ToString().Should().Be($"(\"QualityProfiles\".\"Id\" = @Clause1_P1)");
            _subject.Parameters.Get<int>("Clause1_P1").Should().Be(1);
        }

        [Test]
        public void postgres_where_throws_without_concrete_condition_if_requiresConcreteCondition()
        {
            Expression<Func<Author, Author, bool>> filter = (x, y) => x.Id == y.Id;
            _subject = new WhereBuilderPostgres(filter, true, 0);
            Assert.Throws<InvalidOperationException>(() => _subject.ToString());
        }

        [Test]
        public void postgres_where_allows_abstract_condition_if_not_requiresConcreteCondition()
        {
            Expression<Func<Author, Author, bool>> filter = (x, y) => x.Id == y.Id;
            _subject = new WhereBuilderPostgres(filter, false, 0);
            _subject.ToString().Should().Be($"(\"Authors\".\"Id\" = \"Authors\".\"Id\")");
        }

        [Test]
        public void postgres_where_string_is_null()
        {
            _subject = Where(x => x.CleanName == null);

            _subject.ToString().Should().Be($"(\"Authors\".\"CleanName\" IS NULL)");
        }

        [Test]
        public void postgres_where_string_is_null_value()
        {
            string cleanName = null;
            _subject = Where(x => x.CleanName == cleanName);

            _subject.ToString().Should().Be($"(\"Authors\".\"CleanName\" IS NULL)");
        }

        [Test]
        public void postgres_where_equal_null_property()
        {
            var author = new Author { CleanName = null };
            _subject = Where(x => x.CleanName == author.CleanName);

            _subject.ToString().Should().Be($"(\"Authors\".\"CleanName\" IS NULL)");
        }

        [Test]
        public void postgres_where_column_contains_string()
        {
            var test = "small";
            _subject = Where(x => x.CleanName.Contains(test));

            _subject.ToString().Should().Be($"(\"Authors\".\"CleanName\" ILIKE '%' || @Clause1_P1 || '%')");
            _subject.Parameters.Get<string>("Clause1_P1").Should().Be(test);
        }

        [Test]
        public void postgres_where_string_contains_column()
        {
            var test = "small";
            _subject = Where(x => test.Contains(x.CleanName));

            _subject.ToString().Should().Be($"(@Clause1_P1 ILIKE '%' || \"Authors\".\"CleanName\" || '%')");
            _subject.Parameters.Get<string>("Clause1_P1").Should().Be(test);
        }

        [Test]
        public void postgres_where_column_starts_with_string()
        {
            var test = "small";
            _subject = Where(x => x.CleanName.StartsWith(test));

            _subject.ToString().Should().Be($"(\"Authors\".\"CleanName\" ILIKE @Clause1_P1 || '%')");
            _subject.Parameters.Get<string>("Clause1_P1").Should().Be(test);
        }

        [Test]
        public void postgres_where_column_ends_with_string()
        {
            var test = "small";
            _subject = Where(x => x.CleanName.EndsWith(test));

            _subject.ToString().Should().Be($"(\"Authors\".\"CleanName\" ILIKE '%' || @Clause1_P1)");
            _subject.Parameters.Get<string>("Clause1_P1").Should().Be(test);
        }

        [Test]
        public void postgres_where_in_list()
        {
            var list = new List<int> { 1, 2, 3 };
            _subject = Where(x => list.Contains(x.Id));

            _subject.ToString().Should().Be($"(\"Authors\".\"Id\" = ANY (('{{1, 2, 3}}')))");
        }

        [Test]
        public void postgres_where_in_list_2()
        {
            var list = new List<int> { 1, 2, 3 };
            _subject = Where(x => x.CleanName == "test" && list.Contains(x.Id));

            _subject.ToString().Should().Be($"((\"Authors\".\"CleanName\" = @Clause1_P1) AND (\"Authors\".\"Id\" = ANY (('{{1, 2, 3}}'))))");
        }

        [Test]
        public void postgres_where_in_string_list()
        {
            var list = new List<string> { "first", "second", "third" };

            _subject = Where(x => list.Contains(x.CleanName));

            _subject.ToString().Should().Be($"(\"Authors\".\"CleanName\" = ANY (@Clause1_P1))");
        }

        [Test]
        public void enum_as_int()
        {
            _subject = WhereMetadata(x => x.Status == AuthorStatusType.Continuing);

            _subject.ToString().Should().Be($"(\"AuthorMetadata\".\"Status\" = @Clause1_P1)");
        }

        [Test]
        public void enum_in_list()
        {
            var allowed = new List<AuthorStatusType> { AuthorStatusType.Continuing, AuthorStatusType.Ended };
            _subject = WhereMetadata(x => allowed.Contains(x.Status));

            _subject.ToString().Should().Be($"(\"AuthorMetadata\".\"Status\" = ANY (@Clause1_P1))");
        }

        [Test]
        public void enum_in_array()
        {
            var allowed = new AuthorStatusType[] { AuthorStatusType.Continuing, AuthorStatusType.Ended };
            _subject = WhereMetadata(x => allowed.Contains(x.Status));

            _subject.ToString().Should().Be($"(\"AuthorMetadata\".\"Status\" = ANY (@Clause1_P1))");
        }
    }
}
