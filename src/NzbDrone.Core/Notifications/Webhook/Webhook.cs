using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class Webhook : WebhookBase<WebhookSettings>
    {
        private readonly IWebhookProxy _proxy;

        public Webhook(IWebhookProxy proxy, IConfigFileProvider configFileProvider)
            : base(configFileProvider)
        {
            _proxy = proxy;
        }

        public override string Link => "https://wiki.servarr.com/readarr/settings#connections";

        public override void OnGrab(GrabMessage message)
        {
            _proxy.SendWebhook(BuildOnGrabPayload(message), Settings);
        }

        public override void OnReleaseImport(BookDownloadMessage message)
        {
            _proxy.SendWebhook(BuildOnReleaseImportPayload(message), Settings);
        }

        public override void OnRename(Author author, List<RenamedBookFile> renamedFiles)
        {
            _proxy.SendWebhook(BuildOnRenamePayload(author, renamedFiles), Settings);
        }

        public override void OnAuthorAdded(Author author)
        {
            _proxy.SendWebhook(BuildOnAuthorAdded(author), Settings);
        }

        public override void OnAuthorDelete(AuthorDeleteMessage deleteMessage)
        {
            _proxy.SendWebhook(BuildOnAuthorDelete(deleteMessage), Settings);
        }

        public override void OnBookDelete(BookDeleteMessage deleteMessage)
        {
            _proxy.SendWebhook(BuildOnBookDelete(deleteMessage), Settings);
        }

        public override void OnBookFileDelete(BookFileDeleteMessage deleteMessage)
        {
            _proxy.SendWebhook(BuildOnBookFileDelete(deleteMessage), Settings);
        }

        public override void OnBookRetag(BookRetagMessage message)
        {
            _proxy.SendWebhook(BuildOnBookRetagPayload(message), Settings);
        }

        public override void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
            _proxy.SendWebhook(BuildHealthPayload(healthCheck), Settings);
        }

        public override void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
        {
            _proxy.SendWebhook(BuildApplicationUpdatePayload(updateMessage), Settings);
        }

        public override string Name => "Webhook";

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(SendWebhookTest());

            return new ValidationResult(failures);
        }

        private ValidationFailure SendWebhookTest()
        {
            try
            {
                _proxy.SendWebhook(BuildTestPayload(), Settings);
            }
            catch (WebhookException ex)
            {
                return new NzbDroneValidationFailure("Url", ex.Message);
            }

            return null;
        }
    }
}
