using System.Collections.Generic;
using NzbDrone.Core.Books.Events;
using NzbDrone.Core.Messaging.Events;

namespace NzbDrone.Core.Books
{
    public interface ISeriesBookLinkService
    {
        List<SeriesBookLink> GetLinksBySeries(int seriesId);
        List<SeriesBookLink> GetLinksBySeriesAndAuthor(int seriesId, string foreignAuthorId);
        List<SeriesBookLink> GetLinksByBook(List<int> bookIds);
        void InsertMany(List<SeriesBookLink> model);
        void UpdateMany(List<SeriesBookLink> model);
        void DeleteMany(List<SeriesBookLink> model);
    }

    public class SeriesBookLinkService : ISeriesBookLinkService,
        IHandle<BookDeletedEvent>
    {
        private readonly ISeriesBookLinkRepository _repo;

        public SeriesBookLinkService(ISeriesBookLinkRepository repo)
        {
            _repo = repo;
        }

        public List<SeriesBookLink> GetLinksBySeries(int seriesId)
        {
            return _repo.GetLinksBySeries(seriesId);
        }

        public List<SeriesBookLink> GetLinksBySeriesAndAuthor(int seriesId, string foreignAuthorId)
        {
            return _repo.GetLinksBySeriesAndAuthor(seriesId, foreignAuthorId);
        }

        public List<SeriesBookLink> GetLinksByBook(List<int> bookIds)
        {
            return _repo.GetLinksByBook(bookIds);
        }

        public void InsertMany(List<SeriesBookLink> model)
        {
            _repo.InsertMany(model);
        }

        public void UpdateMany(List<SeriesBookLink> model)
        {
            _repo.UpdateMany(model);
        }

        public void DeleteMany(List<SeriesBookLink> model)
        {
            _repo.DeleteMany(model);
        }

        public void Handle(BookDeletedEvent message)
        {
            var links = GetLinksByBook(new List<int> { message.Book.Id });
            DeleteMany(links);
        }
    }
}
