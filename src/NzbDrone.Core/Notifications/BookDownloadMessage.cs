using System.Collections.Generic;
using NzbDrone.Core.Books;
using NzbDrone.Core.Download;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Notifications
{
    public class BookDownloadMessage
    {
        public string Message { get; set; }
        public Author Author { get; set; }
        public Book Book { get; set; }
        public List<BookFile> BookFiles { get; set; }
        public List<BookFile> OldFiles { get; set; }
        public DownloadClientItemClientInfo DownloadClientInfo { get; set; }
        public string DownloadId { get; set; }

        public override string ToString()
        {
            return Message;
        }
    }
}
