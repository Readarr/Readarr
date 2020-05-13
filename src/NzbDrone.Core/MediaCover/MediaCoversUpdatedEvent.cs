using NzbDrone.Common.Messaging;
using NzbDrone.Core.Books;

namespace NzbDrone.Core.MediaCover
{
    public class MediaCoversUpdatedEvent : IEvent
    {
        public Author Author { get; set; }
        public Book Album { get; set; }

        public MediaCoversUpdatedEvent(Author author)
        {
            Author = author;
        }

        public MediaCoversUpdatedEvent(Book book)
        {
            Album = book;
        }
    }
}
