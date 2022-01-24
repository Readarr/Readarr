using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Http.Dispatchers;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Security;

namespace NzbDrone.Core.Notifications.Email
{
    public class Email : NotificationBase<EmailSettings>
    {
        private readonly ICertificateValidationService _certificateValidationService;
        private readonly Logger _logger;

        public override string Name => "Email";

        public Email(ICertificateValidationService certificateValidationService, Logger logger)
        {
            _certificateValidationService = certificateValidationService;
            _logger = logger;
        }

        public override string Link => null;

        public override void OnGrab(GrabMessage grabMessage)
        {
            var body = $"{grabMessage.Message} sent to queue.";

            SendEmail(Settings, BOOK_GRABBED_TITLE_BRANDED, body);
        }

        public override void OnReleaseImport(BookDownloadMessage message)
        {
            var body = $"{message.Message} Downloaded and sorted.";

            var paths = Settings.AttachFiles ? message.BookFiles.SelectList(a => a.Path) : null;

            SendEmail(Settings, BOOK_DOWNLOADED_TITLE_BRANDED, body, false, paths);
        }

        public override void OnAuthorDelete(AuthorDeleteMessage deleteMessage)
        {
            var body = deleteMessage.Message;

            SendEmail(Settings, AUTHOR_DELETED_TITlE_BRANDED, body);
        }

        public override void OnBookDelete(BookDeleteMessage deleteMessage)
        {
            var body = deleteMessage.Message;

            SendEmail(Settings, AUTHOR_DELETED_TITlE_BRANDED, body);
        }

        public override void OnBookFileDelete(BookFileDeleteMessage deleteMessage)
        {
            var body = deleteMessage.Message;

            SendEmail(Settings, AUTHOR_DELETED_TITlE_BRANDED, body);
        }

        public override void OnHealthIssue(HealthCheck.HealthCheck message)
        {
            SendEmail(Settings, HEALTH_ISSUE_TITLE_BRANDED, message.Message);
        }

        public override void OnDownloadFailure(DownloadFailedMessage message)
        {
            SendEmail(Settings, DOWNLOAD_FAILURE_TITLE_BRANDED, message.Message);
        }

        public override void OnImportFailure(BookDownloadMessage message)
        {
            SendEmail(Settings, IMPORT_FAILURE_TITLE_BRANDED, message.Message);
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(Test(Settings));

            return new ValidationResult(failures);
        }

        public ValidationFailure Test(EmailSettings settings)
        {
            const string body = "Success! You have properly configured your email notification settings";

            try
            {
                SendEmail(settings, "Readarr - Test Notification", body);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Unable to send test email");
                return new ValidationFailure("Server", "Unable to send test email");
            }

            return null;
        }

        private void SendEmail(EmailSettings settings, string subject, string body, bool htmlBody = false, List<string> attachmentUrls = null)
        {
            var email = new MimeMessage();

            email.From.Add(ParseAddress("From", settings.From));
            email.To.AddRange(settings.To.Select(x => ParseAddress("To", x)));
            email.Cc.AddRange(settings.CC.Select(x => ParseAddress("CC", x)));
            email.Bcc.AddRange(settings.Bcc.Select(x => ParseAddress("BCC", x)));

            email.Subject = subject;
            email.Body = new TextPart(htmlBody ? "html" : "plain")
            {
                Text = body
            };

            if (attachmentUrls != null)
            {
                var builder = new BodyBuilder();
                builder.HtmlBody = body;
                foreach (var url in attachmentUrls)
                {
                    if (MediaFileExtensions.TextExtensions.Contains(System.IO.Path.GetExtension(url)))
                    {
                        byte[] bytes = System.IO.File.ReadAllBytes(url);
                        builder.Attachments.Add(url, bytes);
                        _logger.Trace("Attaching ebook file: {0}", url);
                    }
                    else
                    {
                        _logger.Trace("Skipping audiobook file: {0}", url);
                    }
                }

                email.Body = builder.ToMessageBody();
            }

            _logger.Debug("Sending email Subject: {0}", subject);
            try
            {
                Send(email, settings);
                _logger.Debug("Email sent. Subject: {0}", subject);
            }
            catch (Exception ex)
            {
                _logger.Error("Error sending email. Subject: {0}", email.Subject);
                _logger.Debug(ex, ex.Message);
                throw;
            }

            _logger.Debug("Finished sending email. Subject: {0}", email.Subject);
        }

        private void Send(MimeMessage email, EmailSettings settings)
        {
            using (var client = new SmtpClient())
            {
                client.Timeout = 10000;

                var serverOption = SecureSocketOptions.Auto;

                if (settings.RequireEncryption)
                {
                    if (settings.Port == 465)
                    {
                        serverOption = SecureSocketOptions.SslOnConnect;
                    }
                    else
                    {
                        serverOption = SecureSocketOptions.StartTls;
                    }
                }

                client.ServerCertificateValidationCallback = _certificateValidationService.ShouldByPassValidationError;

                _logger.Debug("Connecting to mail server");

                client.Connect(settings.Server, settings.Port, serverOption);

                if (!string.IsNullOrWhiteSpace(settings.Username))
                {
                    _logger.Debug("Authenticating to mail server");

                    client.Authenticate(settings.Username, settings.Password);
                }

                _logger.Debug("Sending to mail server");

                client.Send(email);

                _logger.Debug("Sent to mail server, disconnecting");

                client.Disconnect(true);

                _logger.Debug("Disconnecting from mail server");
            }
        }

        private MailboxAddress ParseAddress(string type, string address)
        {
            try
            {
                return MailboxAddress.Parse(address);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "{0} email address '{1}' invalid", type, address);
                throw;
            }
        }
    }
}
