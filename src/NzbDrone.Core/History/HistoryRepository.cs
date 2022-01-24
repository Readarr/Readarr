using System;
using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Books;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.History
{
    public interface IHistoryRepository : IBasicRepository<EntityHistory>
    {
        EntityHistory MostRecentForBook(int bookId);
        EntityHistory MostRecentForDownloadId(string downloadId);
        List<EntityHistory> FindByDownloadId(string downloadId);
        List<EntityHistory> GetByAuthor(int authorId, EntityHistoryEventType? eventType);
        List<EntityHistory> GetByBook(int bookId, EntityHistoryEventType? eventType);
        List<EntityHistory> FindDownloadHistory(int idAuthorId, QualityModel quality);
        void DeleteForAuthor(int authorId);
        List<EntityHistory> Since(DateTime date, EntityHistoryEventType? eventType);
    }

    public class HistoryRepository : BasicRepository<EntityHistory>, IHistoryRepository
    {
        public HistoryRepository(IMainDatabase database, IEventAggregator eventAggregator)
            : base(database, eventAggregator)
        {
        }

        public EntityHistory MostRecentForBook(int bookId)
        {
            return Query(h => h.BookId == bookId)
                .OrderByDescending(h => h.Date)
                .FirstOrDefault();
        }

        public EntityHistory MostRecentForDownloadId(string downloadId)
        {
            return Query(h => h.DownloadId == downloadId)
                .OrderByDescending(h => h.Date)
                .FirstOrDefault();
        }

        public List<EntityHistory> FindByDownloadId(string downloadId)
        {
            return _database.QueryJoined<EntityHistory, Author, Book>(
                Builder()
                .Join<EntityHistory, Author>((h, a) => h.AuthorId == a.Id)
                .Join<EntityHistory, Book>((h, a) => h.BookId == a.Id)
                .Where<EntityHistory>(h => h.DownloadId == downloadId),
                (history, author, book) =>
                {
                    history.Author = author;
                    history.Book = book;
                    return history;
                }).ToList();
        }

        public List<EntityHistory> GetByAuthor(int authorId, EntityHistoryEventType? eventType)
        {
            var builder = Builder().Where<EntityHistory>(h => h.AuthorId == authorId);

            if (eventType.HasValue)
            {
                builder.Where<EntityHistory>(h => h.EventType == eventType);
            }

            return Query(builder).OrderByDescending(h => h.Date).ToList();
        }

        public List<EntityHistory> GetByBook(int bookId, EntityHistoryEventType? eventType)
        {
            var builder = Builder()
                .Join<EntityHistory, Book>((h, a) => h.BookId == a.Id)
                .Where<EntityHistory>(h => h.BookId == bookId);

            if (eventType.HasValue)
            {
                builder.Where<EntityHistory>(h => h.EventType == eventType);
            }

            return _database.QueryJoined<EntityHistory, Book>(
                builder,
                (history, book) =>
                {
                    history.Book = book;
                    return history;
                }).OrderByDescending(h => h.Date).ToList();
        }

        public List<EntityHistory> FindDownloadHistory(int idAuthorId, QualityModel quality)
        {
            var allowed = new[] { (int)EntityHistoryEventType.Grabbed, (int)EntityHistoryEventType.DownloadFailed, (int)EntityHistoryEventType.BookFileImported };

            return Query(h => h.AuthorId == idAuthorId &&
                         h.Quality == quality &&
                         allowed.Contains((int)h.EventType));
        }

        public void DeleteForAuthor(int authorId)
        {
            Delete(c => c.AuthorId == authorId);
        }

        protected override SqlBuilder PagedBuilder() => new SqlBuilder(_database.DatabaseType)
            .Join<EntityHistory, Author>((h, a) => h.AuthorId == a.Id)
            .Join<Author, AuthorMetadata>((l, r) => l.AuthorMetadataId == r.Id)
            .Join<EntityHistory, Book>((h, a) => h.BookId == a.Id);

        protected override IEnumerable<EntityHistory> PagedQuery(SqlBuilder builder) =>
            _database.QueryJoined<EntityHistory, Author, AuthorMetadata, Book>(builder, (history, author, metadata, book) =>
                    {
                        author.Metadata = metadata;
                        history.Author = author;
                        history.Book = book;
                        return history;
                    });

        public List<EntityHistory> Since(DateTime date, EntityHistoryEventType? eventType)
        {
            var builder = Builder()
                .Join<EntityHistory, Author>((h, a) => h.AuthorId == a.Id)
                .Where<EntityHistory>(x => x.Date >= date);

            if (eventType.HasValue)
            {
                builder.Where<EntityHistory>(h => h.EventType == eventType);
            }

            return _database.QueryJoined<EntityHistory, Author>(builder, (history, author) =>
            {
                history.Author = author;
                return history;
            }).OrderBy(h => h.Date).ToList();
        }
    }
}
