using NzbDrone.Common.Messaging;
using NzbDrone.Core.Books;

namespace NzbDrone.Core.MediaCover
{
    public class MediaCoversUpdatedEvent : IEvent
    {
        public Author Author { get; set; }
        public Book Book { get; set; }
        public bool Updated { get; set; }

        public MediaCoversUpdatedEvent(Author author, bool updated)
        {
            Author = author;
            Updated = updated;
        }

        public MediaCoversUpdatedEvent(Book book, bool updated)
        {
            Book = book;
            Updated = updated;
        }
    }
}
