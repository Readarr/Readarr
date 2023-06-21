using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedSeriesBookLinks : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedSeriesBookLinks(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""SeriesBookLink""
                            WHERE ""Id"" IN (
                            SELECT ""SeriesBookLink"".""Id"" FROM ""SeriesBookLink""
                            LEFT OUTER JOIN ""Books""
                            ON ""SeriesBookLink"".""BookId"" = ""Books"".""Id""
                            WHERE ""Books"".""Id"" IS NULL)");

            mapper.Execute(@"DELETE FROM ""SeriesBookLink""
                             WHERE ""Id"" IN (
                             SELECT ""SeriesBookLink"".""Id"" FROM ""SeriesBookLink""
                             LEFT OUTER JOIN ""Series""
                             ON ""SeriesBookLink"".""SeriesId"" = ""Series"".""Id""
                             WHERE ""Series"".""Id"" IS NULL)");
        }
    }
}
