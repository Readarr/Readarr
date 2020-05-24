using NzbDrone.Common.Messaging;
using NzbDrone.Core.Download.TrackedDownloads;

namespace NzbDrone.Core.Download
{
    public class DownloadCompletedEvent : IEvent
    {
        public TrackedDownload TrackedDownload { get; private set; }
        public int AuthorId { get; set; }

        public DownloadCompletedEvent(TrackedDownload trackedDownload, int authorId)
        {
            TrackedDownload = trackedDownload;
            AuthorId = authorId;
        }
    }
}
