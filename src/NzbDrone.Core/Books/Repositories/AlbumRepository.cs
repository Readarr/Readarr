using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Music
{
    public interface IAlbumRepository : IBasicRepository<Book>
    {
        List<Book> GetAlbums(int artistId);
        List<Book> GetLastAlbums(IEnumerable<int> artistMetadataIds);
        List<Book> GetNextAlbums(IEnumerable<int> artistMetadataIds);
        List<Book> GetAlbumsByArtistMetadataId(int artistMetadataId);
        List<Book> GetAlbumsForRefresh(int artistMetadataId, IEnumerable<string> foreignIds);
        Book FindByTitle(int artistMetadataId, string title);
        Book FindById(string foreignAlbumId);
        PagingSpec<Book> AlbumsWithoutFiles(PagingSpec<Book> pagingSpec);
        PagingSpec<Book> AlbumsWhereCutoffUnmet(PagingSpec<Book> pagingSpec, List<QualitiesBelowCutoff> qualitiesBelowCutoff);
        List<Book> AlbumsBetweenDates(DateTime startDate, DateTime endDate, bool includeUnmonitored);
        List<Book> ArtistAlbumsBetweenDates(Author artist, DateTime startDate, DateTime endDate, bool includeUnmonitored);
        void SetMonitoredFlat(Book album, bool monitored);
        void SetMonitored(IEnumerable<int> ids, bool monitored);
        void SetFileId(List<Book> books);
        Book FindAlbumByRelease(string albumReleaseId);
        Book FindAlbumByTrack(int trackId);
        List<Book> GetArtistAlbumsWithFiles(Author artist);
    }

    public class AlbumRepository : BasicRepository<Book>, IAlbumRepository
    {
        public AlbumRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public List<Book> GetAlbums(int artistId)
        {
            return Query(Builder().Join<Book, Author>((l, r) => l.AuthorMetadataId == r.AuthorMetadataId).Where<Author>(a => a.Id == artistId));
        }

        public List<Book> GetLastAlbums(IEnumerable<int> artistMetadataIds)
        {
            var now = DateTime.UtcNow;
            return Query(Builder().Where<Book>(x => artistMetadataIds.Contains(x.AuthorMetadataId) && x.ReleaseDate < now)
                         .GroupBy<Book>(x => x.AuthorMetadataId)
                         .Having("Books.ReleaseDate = MAX(Books.ReleaseDate)"));
        }

        public List<Book> GetNextAlbums(IEnumerable<int> artistMetadataIds)
        {
            var now = DateTime.UtcNow;
            return Query(Builder().Where<Book>(x => artistMetadataIds.Contains(x.AuthorMetadataId) && x.ReleaseDate > now)
                         .GroupBy<Book>(x => x.AuthorMetadataId)
                         .Having("Books.ReleaseDate = MIN(Books.ReleaseDate)"));
        }

        public List<Book> GetAlbumsByArtistMetadataId(int artistMetadataId)
        {
            return Query(s => s.AuthorMetadataId == artistMetadataId);
        }

        public List<Book> GetAlbumsForRefresh(int artistMetadataId, IEnumerable<string> foreignIds)
        {
            return Query(a => a.AuthorMetadataId == artistMetadataId || foreignIds.Contains(a.ForeignBookId));
        }

        public Book FindById(string foreignAlbumId)
        {
            return Query(s => s.ForeignBookId == foreignAlbumId).SingleOrDefault();
        }

        //x.Id == null is converted to SQL, so warning incorrect
#pragma warning disable CS0472
        private SqlBuilder AlbumsWithoutFilesBuilder(DateTime currentTime, bool monitored) => Builder()
            .Join<Book, Author>((l, r) => l.AuthorMetadataId == r.AuthorMetadataId)
            .LeftJoin<Book, TrackFile>((t, f) => t.BookFileId == f.Id)
            .Where<TrackFile>(f => f.Id == null)
            .Where<Book>(a => a.ReleaseDate <= currentTime && a.Monitored == monitored)
            .Where<Author>(a => a.Monitored == monitored);
#pragma warning restore CS0472

        public PagingSpec<Book> AlbumsWithoutFiles(PagingSpec<Book> pagingSpec)
        {
            var currentTime = DateTime.UtcNow;
            var monitored = pagingSpec.FilterExpressions.FirstOrDefault().ToString().Contains("True");

            pagingSpec.Records = GetPagedRecords(AlbumsWithoutFilesBuilder(currentTime, monitored), pagingSpec, PagedQuery);
            pagingSpec.TotalRecords = GetPagedRecordCount(AlbumsWithoutFilesBuilder(currentTime, monitored).SelectCountDistinct<Book>(x => x.Id), pagingSpec);

            return pagingSpec;
        }

        private SqlBuilder AlbumsWhereCutoffUnmetBuilder(bool monitored, List<QualitiesBelowCutoff> qualitiesBelowCutoff) => Builder()
            .Join<Book, Author>((l, r) => l.AuthorMetadataId == r.AuthorMetadataId)
            .Join<Book, TrackFile>((t, f) => t.BookFileId == f.Id)
            .Where<Book>(a => a.Monitored == monitored)
            .Where<Author>(a => a.Monitored == monitored)
            .Where(BuildQualityCutoffWhereClause(qualitiesBelowCutoff));

        private string BuildQualityCutoffWhereClause(List<QualitiesBelowCutoff> qualitiesBelowCutoff)
        {
            var clauses = new List<string>();

            foreach (var profile in qualitiesBelowCutoff)
            {
                foreach (var belowCutoff in profile.QualityIds)
                {
                    clauses.Add(string.Format("(Authors.[QualityProfileId] = {0} AND TrackFiles.Quality LIKE '%_quality_: {1},%')", profile.ProfileId, belowCutoff));
                }
            }

            return string.Format("({0})", string.Join(" OR ", clauses));
        }

        public PagingSpec<Book> AlbumsWhereCutoffUnmet(PagingSpec<Book> pagingSpec, List<QualitiesBelowCutoff> qualitiesBelowCutoff)
        {
            var monitored = pagingSpec.FilterExpressions.FirstOrDefault().ToString().Contains("True");

            pagingSpec.Records = GetPagedRecords(AlbumsWhereCutoffUnmetBuilder(monitored, qualitiesBelowCutoff), pagingSpec, PagedQuery);

            var countTemplate = $"SELECT COUNT(*) FROM (SELECT /**select**/ FROM {TableMapping.Mapper.TableNameMapping(typeof(Book))} /**join**/ /**innerjoin**/ /**leftjoin**/ /**where**/ /**groupby**/ /**having**/)";
            pagingSpec.TotalRecords = GetPagedRecordCount(AlbumsWhereCutoffUnmetBuilder(monitored, qualitiesBelowCutoff).Select(typeof(Book)), pagingSpec, countTemplate);

            return pagingSpec;
        }

        public List<Book> AlbumsBetweenDates(DateTime startDate, DateTime endDate, bool includeUnmonitored)
        {
            var builder = Builder().Where<Book>(rg => rg.ReleaseDate >= startDate && rg.ReleaseDate <= endDate);

            if (!includeUnmonitored)
            {
                builder = builder.Where<Book>(e => e.Monitored == true)
                    .Join<Book, Author>((l, r) => l.AuthorMetadataId == r.AuthorMetadataId)
                    .Where<Author>(e => e.Monitored == true);
            }

            return Query(builder);
        }

        public List<Book> ArtistAlbumsBetweenDates(Author artist, DateTime startDate, DateTime endDate, bool includeUnmonitored)
        {
            var builder = Builder().Where<Book>(rg => rg.ReleaseDate >= startDate &&
                                                 rg.ReleaseDate <= endDate &&
                                                 rg.AuthorMetadataId == artist.AuthorMetadataId);

            if (!includeUnmonitored)
            {
                builder = builder.Where<Book>(e => e.Monitored == true)
                    .Join<Book, Author>((l, r) => l.AuthorMetadataId == r.AuthorMetadataId)
                    .Where<Author>(e => e.Monitored == true);
            }

            return Query(builder);
        }

        public void SetMonitoredFlat(Book album, bool monitored)
        {
            album.Monitored = monitored;
            SetFields(album, p => p.Monitored);
        }

        public void SetMonitored(IEnumerable<int> ids, bool monitored)
        {
            var albums = ids.Select(x => new Book { Id = x, Monitored = monitored }).ToList();
            SetFields(albums, p => p.Monitored);
        }

        public void SetFileId(List<Book> books)
        {
            SetFields(books, t => t.BookFileId);
        }

        public Book FindByTitle(int artistMetadataId, string title)
        {
            var cleanTitle = Parser.Parser.CleanArtistName(title);

            if (string.IsNullOrEmpty(cleanTitle))
            {
                cleanTitle = title;
            }

            return Query(s => (s.CleanTitle == cleanTitle || s.Title == title) && s.AuthorMetadataId == artistMetadataId)
                .ExclusiveOrDefault();
        }

        public Book FindAlbumByRelease(string albumReleaseId)
        {
            return Query(Builder().Join<Book, AlbumRelease>((a, r) => a.Id == r.AlbumId)
                         .Where<AlbumRelease>(x => x.ForeignReleaseId == albumReleaseId)).FirstOrDefault();
        }

        public Book FindAlbumByTrack(int trackId)
        {
            return Query(Builder().Join<Book, AlbumRelease>((a, r) => a.Id == r.AlbumId)
                         .Join<AlbumRelease, Track>((r, t) => r.Id == t.AlbumReleaseId)
                         .Where<Track>(x => x.Id == trackId)).FirstOrDefault();
        }

        public List<Book> GetArtistAlbumsWithFiles(Author artist)
        {
            return Query(Builder()
                         .Join<Book, TrackFile>((t, f) => t.BookFileId == f.Id)
                         .Where<Book>(x => x.AuthorMetadataId == artist.AuthorMetadataId));
        }
    }
}
