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

            if (_database.DatabaseType == DatabaseType.PostgreSQL)
            {
                mapper.Execute(@"UPDATE ""Editions""
                                SET ""Monitored"" = true
                                WHERE ""Id"" IN (
                                    SELECT MIN(""Id"")
                                    FROM ""Editions""
                                    WHERE ""Monitored"" = true
                                    GROUP BY ""BookId""
                                    HAVING COUNT(""BookId"") > 1
                                )");
            }
            else
            {
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
}
