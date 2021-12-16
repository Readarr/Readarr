using System.Collections.Generic;
using System.Net.Sockets;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;

namespace NzbDrone.Core.Notifications.Subsonic
{
    public class Subsonic : NotificationBase<SubsonicSettings>
    {
        private readonly ISubsonicService _subsonicService;
        private readonly Logger _logger;

        public Subsonic(ISubsonicService subsonicService, Logger logger)
        {
            _subsonicService = subsonicService;
            _logger = logger;
        }

        public override string Link => "http://subsonic.org/";

        public override void OnGrab(GrabMessage grabMessage)
        {
            const string header = "Readarr - Grabbed";

            Notify(Settings, header, grabMessage.Message);
        }

        public override void OnReleaseImport(BookDownloadMessage message)
        {
            const string header = "Readarr - Downloaded";

            Notify(Settings, header, message.Message);
            Update();
        }

        public override void OnRename(Author author)
        {
            Update();
        }

        public override void OnAuthorDelete(AuthorDeleteMessage deleteMessage)
        {
            const string header = "Readarr - Author Deleted";

            Notify(Settings, header, deleteMessage.Message);

            if (deleteMessage.DeletedFiles)
            {
                Update();
            }
        }

        public override void OnBookDelete(BookDeleteMessage deleteMessage)
        {
            const string header = "Readarr - Book Deleted";

            Notify(Settings, header, deleteMessage.Message);

            if (deleteMessage.DeletedFiles)
            {
                Update();
            }
        }

        public override void OnBookFileDelete(BookFileDeleteMessage deleteMessage)
        {
            const string header = "Readarr - Book File Deleted";

            Notify(Settings, header, deleteMessage.Message);
            Update();
        }

        public override void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
            Notify(Settings, HEALTH_ISSUE_TITLE_BRANDED, healthCheck.Message);
        }

        public override void OnBookRetag(BookRetagMessage message)
        {
            Notify(Settings, BOOK_RETAGGED_TITLE_BRANDED, message.Message);
        }

        public override string Name => "Subsonic";

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(_subsonicService.Test(Settings, "Success! Subsonic has been successfully configured!"));

            return new ValidationResult(failures);
        }

        private void Notify(SubsonicSettings settings, string header, string message)
        {
            try
            {
                if (Settings.Notify)
                {
                    _subsonicService.Notify(Settings, $"{header} - {message}");
                }
            }
            catch (SocketException ex)
            {
                var logMessage = $"Unable to connect to Subsonic Host: {Settings.Host}:{Settings.Port}";
                _logger.Debug(ex, logMessage);
            }
        }

        private void Update()
        {
            try
            {
                if (Settings.UpdateLibrary)
                {
                    _subsonicService.Update(Settings);
                }
            }
            catch (SocketException ex)
            {
                var logMessage = $"Unable to connect to Subsonic Host: {Settings.Host}:{Settings.Port}";
                _logger.Debug(ex, logMessage);
            }
        }
    }
}
