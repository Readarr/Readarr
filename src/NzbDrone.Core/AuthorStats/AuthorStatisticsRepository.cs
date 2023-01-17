using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using NzbDrone.Core.Books;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.AuthorStats
{
    public interface IAuthorStatisticsRepository
    {
        List<BookStatistics> AuthorStatistics();
        List<BookStatistics> AuthorStatistics(int authorId);
    }

    public class AuthorStatisticsRepository : IAuthorStatisticsRepository
    {
        private const string _selectTemplate = "SELECT /**select**/ FROM \"Editions\" /**join**/ /**innerjoin**/ /**leftjoin**/ /**where**/ /**groupby**/ /**having**/ /**orderby**/";

        private readonly IMainDatabase _database;

        public AuthorStatisticsRepository(IMainDatabase database)
        {
            _database = database;
        }

        public List<BookStatistics> AuthorStatistics()
        {
            return Query(Builder());
        }

        public List<BookStatistics> AuthorStatistics(int authorId)
        {
            return Query(Builder().Where<Author>(x => x.Id == authorId));
        }

        private List<BookStatistics> Query(SqlBuilder builder)
        {
            var sql = builder.AddTemplate(_selectTemplate).LogQuery();

            using (var conn = _database.OpenConnection())
            {
                return conn.Query<BookStatistics>(sql.RawSql, sql.Parameters).ToList();
            }
        }

        private SqlBuilder Builder()
        {
            var trueIndicator = _database.DatabaseType == DatabaseType.PostgreSQL ? "true" : "1";

            return new SqlBuilder(_database.DatabaseType)
            .Select($@"""Authors"".""Id"" AS ""AuthorId"",
                     ""Books"".""Id"" AS ""BookId"",
                     SUM(COALESCE(""BookFiles"".""Size"", 0)) AS ""SizeOnDisk"",
                     1 AS ""TotalBookCount"",
                     CASE WHEN MIN(""BookFiles"".""Id"") IS NULL THEN 0 ELSE 1 END AS ""AvailableBookCount"",
                     CASE WHEN (""Books"".""Monitored"" = {trueIndicator} AND (""Books"".""ReleaseDate"" < @currentDate) OR ""Books"".""ReleaseDate"" IS NULL) OR MIN(""BookFiles"".""Id"") IS NOT NULL THEN 1 ELSE 0 END AS ""BookCount"",
                     CASE WHEN MIN(""BookFiles"".""Id"") IS NULL THEN 0 ELSE COUNT(""BookFiles"".""Id"") END AS ""BookFileCount""")
            .Join<Edition, Book>((e, b) => e.BookId == b.Id)
            .Join<Book, Author>((book, author) => book.AuthorMetadataId == author.AuthorMetadataId)
            .LeftJoin<Edition, BookFile>((t, f) => t.Id == f.EditionId)
            .Where<Edition>(x => x.Monitored == true)
            .GroupBy<Author>(x => x.Id)
            .GroupBy<Book>(x => x.Id)
            .AddParameters(new Dictionary<string, object> { { "currentDate", DateTime.UtcNow } });
        }
    }
}
