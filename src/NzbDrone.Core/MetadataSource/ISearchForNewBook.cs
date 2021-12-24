using System.Collections.Generic;
using NzbDrone.Core.Books;

namespace NzbDrone.Core.MetadataSource
{
    public interface ISearchForNewBook
    {
        List<Book> SearchForNewBook(string title, string author);
        List<Book> SearchByIsbn(string isbn);
        List<Book> SearchByAsin(string asin);
        List<Book> SearchByGoodreadsWorkId(int goodreadsId);
        List<Book> SearchByGoodreadsBookId(int goodreadsId);
    }
}
