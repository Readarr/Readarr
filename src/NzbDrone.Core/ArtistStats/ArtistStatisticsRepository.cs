using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.ArtistStats
{
    public interface IArtistStatisticsRepository
    {
        List<AlbumStatistics> ArtistStatistics();
        List<AlbumStatistics> ArtistStatistics(int artistId);
    }

    public class ArtistStatisticsRepository : IArtistStatisticsRepository
    {
        private const string _selectTemplate = "SELECT /**select**/ FROM Books /**join**/ /**innerjoin**/ /**leftjoin**/ /**where**/ /**groupby**/ /**having**/ /**orderby**/";

        private readonly IMainDatabase _database;

        public ArtistStatisticsRepository(IMainDatabase database)
        {
            _database = database;
        }

        public List<AlbumStatistics> ArtistStatistics()
        {
            var time = DateTime.UtcNow;
            return Query(Builder().Where<Book>(x => x.ReleaseDate < time));
        }

        public List<AlbumStatistics> ArtistStatistics(int artistId)
        {
            var time = DateTime.UtcNow;
            return Query(Builder().Where<Book>(x => x.ReleaseDate < time)
                         .Where<Author>(x => x.Id == artistId));
        }

        private List<AlbumStatistics> Query(SqlBuilder builder)
        {
            var sql = builder.AddTemplate(_selectTemplate).LogQuery();

            using (var conn = _database.OpenConnection())
            {
                return conn.Query<AlbumStatistics>(sql.RawSql, sql.Parameters).ToList();
            }
        }

        private SqlBuilder Builder() => new SqlBuilder()
            .Select(@"Authors.Id AS ArtistId,
                     Books.Id AS AlbumId,
                     SUM(COALESCE(TrackFiles.Size, 0)) AS SizeOnDisk,
                     COUNT(Books.Id) AS TotalTrackCount,
                     SUM(CASE WHEN Books.BookFileId > 0 THEN 1 ELSE 0 END) AS AvailableTrackCount,
                     SUM(CASE WHEN Books.Monitored = 1 OR Books.BookFileId > 0 THEN 1 ELSE 0 END) AS TrackCount,
                     SUM(CASE WHEN TrackFiles.Id IS NULL THEN 0 ELSE 1 END) AS TrackFileCount")
            .Join<Book, Author>((album, artist) => album.AuthorMetadataId == artist.AuthorMetadataId)
            .LeftJoin<Book, TrackFile>((t, f) => t.BookFileId == f.Id)
            .GroupBy<Author>(x => x.Id)
            .GroupBy<Book>(x => x.Id);
    }
}
