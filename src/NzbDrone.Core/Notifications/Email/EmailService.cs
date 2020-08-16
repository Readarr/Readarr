using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using FluentValidation.Results;
using NLog;
using NzbDrone.Common.Http;

namespace NzbDrone.Core.Notifications.Email
{
    public interface IEmailService
    {
        void SendEmail(EmailSettings settings, string subject, string body, bool htmlBody = false, List<string> attachmentUrls = null);
        ValidationFailure Test(EmailSettings settings);
    }

    public class EmailService : IEmailService
    {
        private readonly Logger _logger;

        public EmailService(Logger logger)
        {
            _logger = logger;
        }

        public void SendEmail(EmailSettings settings, string subject, string body, bool htmlBody = false, List<string> attachmentUrls = null)
        {
            var email = new MailMessage();
            email.From = new MailAddress(settings.From);

            settings.To.ToList().ForEach(x => email.To.Add(x));
            settings.CC.ToList().ForEach(x => email.CC.Add(x));
            settings.Bcc.ToList().ForEach(x => email.Bcc.Add(x));

            email.Subject = subject;
            email.Body = body;
            email.IsBodyHtml = htmlBody;

            if (attachmentUrls != null)
            {
                foreach (var url in attachmentUrls)
                {
                    email.Attachments.Add(new Attachment(url));
                }
            }

            BasicNetworkCredential credentials = null;

            if (!string.IsNullOrWhiteSpace(settings.Username))
            {
                credentials = new BasicNetworkCredential(settings.Username, settings.Password);
            }

            try
            {
                Send(email, settings.Server, settings.Port, settings.Ssl, credentials);
            }
            catch (Exception ex)
            {
                _logger.Error("Error sending email. Subject: {0}", email.Subject);
                _logger.Debug(ex, ex.Message);
                throw;
            }
        }

        private void Send(MailMessage email, string server, int port, bool ssl, BasicNetworkCredential credentials)
        {
            var smtp = new SmtpClient(server, port);
            smtp.EnableSsl = ssl;
            smtp.Credentials = credentials;

            smtp.Send(email);
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
    }
}
