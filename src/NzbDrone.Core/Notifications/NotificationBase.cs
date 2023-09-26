using System;
using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Core.Books;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.ThingiProvider;

namespace NzbDrone.Core.Notifications
{
    public abstract class NotificationBase<TSettings> : INotification
        where TSettings : IProviderConfig, new()
    {
        protected const string BOOK_GRABBED_TITLE = "Book Grabbed";
        protected const string BOOK_DOWNLOADED_TITLE = "Book Downloaded";
        protected const string AUTHOR_ADDED_TITLE = "Author Added";
        protected const string AUTHOR_DELETED_TITLE = "Author Deleted";
        protected const string BOOK_DELETED_TITLE = "Book Deleted";
        protected const string BOOK_FILE_DELETED_TITLE = "Book File Deleted";
        protected const string HEALTH_ISSUE_TITLE = "Health Check Failure";
        protected const string DOWNLOAD_FAILURE_TITLE = "Download Failed";
        protected const string IMPORT_FAILURE_TITLE = "Import Failed";
        protected const string BOOK_RETAGGED_TITLE = "Book File Tags Updated";
        protected const string APPLICATION_UPDATE_TITLE = "Application Updated";

        protected const string BOOK_GRABBED_TITLE_BRANDED = "Readarr - " + BOOK_GRABBED_TITLE;
        protected const string BOOK_DOWNLOADED_TITLE_BRANDED = "Readarr - " + BOOK_DOWNLOADED_TITLE;
        protected const string AUTHOR_ADDED_TITLE_BRANDED = "Readarr - " + AUTHOR_ADDED_TITLE;
        protected const string AUTHOR_DELETED_TITlE_BRANDED = "Readarr - " + AUTHOR_DELETED_TITLE;
        protected const string BOOK_DELETED_TITLE_BRANDED = "Readarr - " + BOOK_DELETED_TITLE;
        protected const string BOOK_FILE_DELETED_TITLE_BRANDED = "Readarr - " + BOOK_FILE_DELETED_TITLE;
        protected const string HEALTH_ISSUE_TITLE_BRANDED = "Readarr - " + HEALTH_ISSUE_TITLE;
        protected const string DOWNLOAD_FAILURE_TITLE_BRANDED = "Readarr - " + DOWNLOAD_FAILURE_TITLE;
        protected const string IMPORT_FAILURE_TITLE_BRANDED = "Readarr - " + IMPORT_FAILURE_TITLE;
        protected const string BOOK_RETAGGED_TITLE_BRANDED = "Readarr - " + BOOK_RETAGGED_TITLE;
        protected const string APPLICATION_UPDATE_TITLE_BRANDED = "Readarr - " + APPLICATION_UPDATE_TITLE;

        public abstract string Name { get; }

        public Type ConfigContract => typeof(TSettings);

        public virtual ProviderMessage Message => null;

        public IEnumerable<ProviderDefinition> DefaultDefinitions => new List<ProviderDefinition>();

        public ProviderDefinition Definition { get; set; }
        public abstract ValidationResult Test();

        public abstract string Link { get; }

        public virtual void OnGrab(GrabMessage grabMessage)
        {
        }

        public virtual void OnReleaseImport(BookDownloadMessage message)
        {
        }

        public virtual void OnRename(Author author, List<RenamedBookFile> renamedFiles)
        {
        }

        public virtual void OnAuthorAdded(Author author)
        {
        }

        public virtual void OnAuthorDelete(AuthorDeleteMessage deleteMessage)
        {
        }

        public virtual void OnBookDelete(BookDeleteMessage deleteMessage)
        {
        }

        public virtual void OnBookFileDelete(BookFileDeleteMessage deleteMessage)
        {
        }

        public virtual void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
        }

        public virtual void OnDownloadFailure(DownloadFailedMessage message)
        {
        }

        public virtual void OnImportFailure(BookDownloadMessage message)
        {
        }

        public virtual void OnBookRetag(BookRetagMessage message)
        {
        }

        public virtual void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
        {
        }

        public virtual void ProcessQueue()
        {
        }

        public bool SupportsOnGrab => HasConcreteImplementation("OnGrab");
        public bool SupportsOnRename => HasConcreteImplementation("OnRename");
        public bool SupportsOnAuthorAdded => HasConcreteImplementation("OnAuthorAdded");
        public bool SupportsOnAuthorDelete => HasConcreteImplementation("OnAuthorDelete");
        public bool SupportsOnBookDelete => HasConcreteImplementation("OnBookDelete");
        public bool SupportsOnBookFileDelete => HasConcreteImplementation("OnBookFileDelete");
        public bool SupportsOnBookFileDeleteForUpgrade => SupportsOnBookFileDelete;
        public bool SupportsOnReleaseImport => HasConcreteImplementation("OnReleaseImport");
        public bool SupportsOnUpgrade => SupportsOnReleaseImport;
        public bool SupportsOnHealthIssue => HasConcreteImplementation("OnHealthIssue");
        public bool SupportsOnDownloadFailure => HasConcreteImplementation("OnDownloadFailure");
        public bool SupportsOnImportFailure => HasConcreteImplementation("OnImportFailure");
        public bool SupportsOnBookRetag => HasConcreteImplementation("OnBookRetag");
        public bool SupportsOnApplicationUpdate => HasConcreteImplementation("OnApplicationUpdate");

        protected TSettings Settings => (TSettings)Definition.Settings;

        public override string ToString()
        {
            return GetType().Name;
        }

        public virtual object RequestAction(string action, IDictionary<string, string> query)
        {
            return null;
        }

        private bool HasConcreteImplementation(string methodName)
        {
            var method = GetType().GetMethod(methodName);

            if (method == null)
            {
                throw new MissingMethodException(GetType().Name, Name);
            }

            return !method.DeclaringType.IsAbstract;
        }
    }
}
