using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedEditions : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedEditions(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            using var mapper = _database.OpenConnection();
            mapper.Execute(@"DELETE FROM ""Editions""
                             WHERE ""Id"" IN (
                             SELECT ""Editions"".""Id"" FROM ""Editions""
                             LEFT OUTER JOIN ""Books""
                             ON ""Editions"".""BookId"" = ""Books"".""Id""
                             WHERE ""Books"".""Id"" IS NULL)");
        }
    }
}
