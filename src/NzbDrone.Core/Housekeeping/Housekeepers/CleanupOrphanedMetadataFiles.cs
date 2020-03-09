using Dapper;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Housekeeping.Housekeepers
{
    public class CleanupOrphanedMetadataFiles : IHousekeepingTask
    {
        private readonly IMainDatabase _database;

        public CleanupOrphanedMetadataFiles(IMainDatabase database)
        {
            _database = database;
        }

        public void Clean()
        {
            DeleteOrphanedByArtist();
            DeleteOrphanedByAlbum();
            DeleteOrphanedByTrackFile();
            DeleteWhereBookIdIsZero();
            DeleteWhereTrackFileIsZero();
        }

        private void DeleteOrphanedByArtist()
        {
            using (var mapper = _database.OpenConnection())
            {
                mapper.Execute(@"DELETE FROM MetadataFiles
                                     WHERE Id IN (
                                     SELECT MetadataFiles.Id FROM MetadataFiles
                                     LEFT OUTER JOIN Artists
                                     ON MetadataFiles.AuthorId = Artists.Id
                                     WHERE Artists.Id IS NULL)");
            }
        }

        private void DeleteOrphanedByAlbum()
        {
            using (var mapper = _database.OpenConnection())
            {
                mapper.Execute(@"DELETE FROM MetadataFiles
                                     WHERE Id IN (
                                     SELECT MetadataFiles.Id FROM MetadataFiles
                                     LEFT OUTER JOIN Albums
                                     ON MetadataFiles.BookId = Albums.Id
                                     WHERE MetadataFiles.BookId > 0
                                     AND Albums.Id IS NULL)");
            }
        }

        private void DeleteOrphanedByTrackFile()
        {
            using (var mapper = _database.OpenConnection())
            {
                mapper.Execute(@"DELETE FROM MetadataFiles
                                     WHERE Id IN (
                                     SELECT MetadataFiles.Id FROM MetadataFiles
                                     LEFT OUTER JOIN TrackFiles
                                     ON MetadataFiles.TrackFileId = TrackFiles.Id
                                     WHERE MetadataFiles.TrackFileId > 0
                                     AND TrackFiles.Id IS NULL)");
            }
        }

        private void DeleteWhereBookIdIsZero()
        {
            using (var mapper = _database.OpenConnection())
            {
                mapper.Execute(@"DELETE FROM MetadataFiles
                                     WHERE Id IN (
                                     SELECT Id FROM MetadataFiles
                                     WHERE Type IN (4, 6)
                                     AND BookId = 0)");
            }
        }

        private void DeleteWhereTrackFileIsZero()
        {
            using (var mapper = _database.OpenConnection())
            {
                mapper.Execute(@"DELETE FROM MetadataFiles
                                     WHERE Id IN (
                                     SELECT Id FROM MetadataFiles
                                     WHERE Type IN (2, 5)
                                     AND TrackFileId = 0)");
            }
        }
    }
}
