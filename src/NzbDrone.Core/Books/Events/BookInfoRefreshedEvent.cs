using System.Collections.Generic;
using System.Collections.ObjectModel;
using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Books.Events
{
    public class BookInfoRefreshedEvent : IEvent
    {
        public Author Author { get; set; }
        public ReadOnlyCollection<Book> Added { get; private set; }

        public ReadOnlyCollection<Book> Updated { get; private set; }
        public ReadOnlyCollection<Book> Removed { get; private set; }

        public BookInfoRefreshedEvent(Author author, IList<Book> added, IList<Book> updated, IList<Book> removed)
        {
            Author = author;
            Added = new ReadOnlyCollection<Book>(added);
            Updated = new ReadOnlyCollection<Book>(updated);
            Removed = new ReadOnlyCollection<Book>(removed);
        }
    }
}
