using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class FixMultipleMonitoredEditions : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public FixMultipleMonitoredEditions(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            using var mapper = _database.OpenConnection();

            mapper.Execute(@"UPDATE ""Editions""
                            SET ""Monitored"" = 0
                            WHERE ""Id"" IN (
                                SELECT MIN(""Id"")
                                FROM ""Editions""
                                WHERE ""Monitored"" = 1
                                GROUP BY ""BookId""
                                HAVING COUNT(""BookId"") > 1
                            )");
        }
    }
}
