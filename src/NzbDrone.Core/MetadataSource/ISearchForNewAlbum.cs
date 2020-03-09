using System.Collections.Generic;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.MetadataSource
{
    public interface ISearchForNewBook
    {
        List<Book> SearchForNewBook(string title, string artist);
        List<Book> SearchByIsbn(string isbn);
        List<Book> SearchForNewAlbumByRecordingIds(List<string> recordingIds);
    }
}
