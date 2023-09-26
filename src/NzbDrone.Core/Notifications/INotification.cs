using System.Collections.Generic;
using NzbDrone.Core.Books;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Notifications
{
    public interface INotification : IProvider
    {
        string Link { get; }

        void OnGrab(GrabMessage grabMessage);
        void OnReleaseImport(BookDownloadMessage message);
        void OnRename(Author author, List<RenamedBookFile> renamedFiles);
        void OnAuthorAdded(Author author);
        void OnAuthorDelete(AuthorDeleteMessage deleteMessage);
        void OnBookDelete(BookDeleteMessage deleteMessage);
        void OnBookFileDelete(BookFileDeleteMessage deleteMessage);
        void OnHealthIssue(HealthCheck.HealthCheck healthCheck);
        void OnApplicationUpdate(ApplicationUpdateMessage updateMessage);
        void OnDownloadFailure(DownloadFailedMessage message);
        void OnImportFailure(BookDownloadMessage message);
        void OnBookRetag(BookRetagMessage message);
        void ProcessQueue();
        bool SupportsOnGrab { get; }
        bool SupportsOnReleaseImport { get; }
        bool SupportsOnUpgrade { get; }
        bool SupportsOnRename { get; }
        bool SupportsOnAuthorAdded { get; }
        bool SupportsOnAuthorDelete { get; }
        bool SupportsOnBookDelete { get; }
        bool SupportsOnBookFileDelete { get; }
        bool SupportsOnBookFileDeleteForUpgrade { get; }
        bool SupportsOnHealthIssue { get; }
        bool SupportsOnApplicationUpdate { get; }
        bool SupportsOnDownloadFailure { get; }
        bool SupportsOnImportFailure { get; }
        bool SupportsOnBookRetag { get; }
    }
}
