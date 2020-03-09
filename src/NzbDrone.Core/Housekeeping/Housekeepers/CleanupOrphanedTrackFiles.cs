using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedBookFiles : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedBookFiles(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            using (var mapper = _database.OpenConnection())
            {
                // Unlink where track no longer exists
                mapper.Execute(@"UPDATE BookFiles
                                     SET BookId = 0
                                     WHERE Id IN (
                                     SELECT BookFiles.Id FROM BookFiles
                                     LEFT OUTER JOIN Books
                                     ON BookFiles.Id = Books.BookFileId
                                     WHERE Books.Id IS NULL)");

                // Unlink Books where the Trackfiles entry no longer exists
                mapper.Execute(@"UPDATE Books
                                     SET BookFileId = 0
                                     WHERE Id IN (
                                     SELECT Books.Id FROM Books
                                     LEFT OUTER JOIN BookFiles
                                     ON Books.BookFileId = BookFiles.Id
                                     WHERE BookFiles.Id IS NULL)");
            }
        }
    }
}
