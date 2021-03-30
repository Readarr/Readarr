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
            using (var mapper = _database.OpenConnection())
            {
                mapper.Execute(@"DELETE FROM SeriesBookLinks
                                     WHERE Id IN (
                                     SELECT SeriesBookLinks.Id FROM SeriesBookLinks
                                     LEFT OUTER JOIN Books
                                     ON SeriesBookLinks.BookId = Books.Id
                                     WHERE Books.Id IS NULL)");

                mapper.Execute(@"DELETE FROM SeriesBookLinks
                                     WHERE Id IN (
                                     SELECT SeriesBookLinks.Id FROM SeriesBookLinks
                                     LEFT OUTER JOIN Series
                                     ON SeriesBookLinks.SeriesId = Series.Id
                                     WHERE Series.Id IS NULL)");
            }
        }
    }
}
