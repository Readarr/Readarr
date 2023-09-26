using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;

namespace NzbDrone.Core.Notifications.Prowl
{
    public class Prowl : NotificationBase<ProwlSettings>
    {
        private readonly IProwlProxy _prowlProxy;

        public Prowl(IProwlProxy prowlProxy)
        {
            _prowlProxy = prowlProxy;
        }

        public override string Link => "https://www.prowlapp.com/";
        public override string Name => "Prowl";

        public override void OnGrab(GrabMessage message)
        {
            _prowlProxy.SendNotification(BOOK_GRABBED_TITLE, message.Message, Settings);
        }

        public override void OnReleaseImport(BookDownloadMessage message)
        {
            _prowlProxy.SendNotification(BOOK_DOWNLOADED_TITLE, message.Message, Settings);
        }

        public override void OnAuthorAdded(Author author)
        {
            _prowlProxy.SendNotification(AUTHOR_ADDED_TITLE, author.Name, Settings);
        }

        public override void OnAuthorDelete(AuthorDeleteMessage deleteMessage)
        {
            _prowlProxy.SendNotification(AUTHOR_DELETED_TITLE, deleteMessage.Message, Settings);
        }

        public override void OnBookDelete(BookDeleteMessage deleteMessage)
        {
            _prowlProxy.SendNotification(BOOK_DELETED_TITLE, deleteMessage.Message, Settings);
        }

        public override void OnBookFileDelete(BookFileDeleteMessage deleteMessage)
        {
            _prowlProxy.SendNotification(BOOK_FILE_DELETED_TITLE, deleteMessage.Message, Settings);
        }

        public override void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
            _prowlProxy.SendNotification(HEALTH_ISSUE_TITLE, healthCheck.Message, Settings);
        }

        public override void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
        {
            _prowlProxy.SendNotification(APPLICATION_UPDATE_TITLE, updateMessage.Message, Settings);
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(_prowlProxy.Test(Settings));

            return new ValidationResult(failures);
        }
    }
}
