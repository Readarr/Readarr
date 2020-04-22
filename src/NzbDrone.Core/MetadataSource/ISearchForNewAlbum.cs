using System.Collections.Generic;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.MetadataSource
{
    public interface ISearchForNewBook
    {
        List<Book> SearchForNewBook(string title, string artist);
        Book SearchByIsbn(string isbn);
        Book SearchByAsin(string asin);
        Book SearchByGoodreadsId(int goodreadsId);
        List<Book> SearchForNewAlbumByRecordingIds(List<string> recordingIds);
    }
}
