using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Notifications.Webhook
{
    public class Webhook : NotificationBase<WebhookSettings>
    {
        private readonly IWebhookProxy _proxy;

        public Webhook(IWebhookProxy proxy)
        {
            _proxy = proxy;
        }

        public override string Link => "https://wiki.servarr.com/readarr/settings#connect";

        public override void OnGrab(GrabMessage message)
        {
            var remoteBook = message.Book;
            var quality = message.Quality;

            var payload = new WebhookGrabPayload
            {
                EventType = WebhookEventType.Grab,
                Author = new WebhookAuthor(message.Author),
                Books = remoteBook.Books.ConvertAll(x => new WebhookBook(x)
                {
                    // TODO: Stop passing these parameters inside an book v3
                    Quality = quality.Quality.Name,
                    QualityVersion = quality.Revision.Version,
                    ReleaseGroup = remoteBook.ParsedBookInfo.ReleaseGroup
                }),
                Release = new WebhookRelease(quality, remoteBook),
                DownloadClient = message.DownloadClient,
                DownloadId = message.DownloadId
            };

            _proxy.SendWebhook(payload, Settings);
        }

        public override void OnReleaseImport(BookDownloadMessage message)
        {
            var bookFiles = message.BookFiles;

            var payload = new WebhookImportPayload
            {
                EventType = WebhookEventType.Download,
                Author = new WebhookAuthor(message.Author),
                Book = new WebhookBook(message.Book),
                BookFiles = bookFiles.ConvertAll(x => new WebhookBookFile(x)),
                IsUpgrade = message.OldFiles.Any(),
                DownloadClient = message.DownloadClient,
                DownloadId = message.DownloadId
            };

            _proxy.SendWebhook(payload, Settings);
        }

        public override void OnRename(Author author)
        {
            var payload = new WebhookRenamePayload
            {
                EventType = WebhookEventType.Rename,
                Author = new WebhookAuthor(author)
            };

            _proxy.SendWebhook(payload, Settings);
        }

        public override void OnAuthorDelete(AuthorDeleteMessage deleteMessage)
        {
            var payload = new WebhookAuthorDeletePayload
            {
                EventType = WebhookEventType.Delete,
                Author = new WebhookAuthor(deleteMessage.Author),
                DeletedFiles = deleteMessage.DeletedFiles
            };

            _proxy.SendWebhook(payload, Settings);
        }

        public override void OnBookDelete(BookDeleteMessage deleteMessage)
        {
            var payload = new WebhookBookDeletePayload
            {
                EventType = WebhookEventType.Delete,
                Author = new WebhookAuthor(deleteMessage.Book.Author),
                Book = new WebhookBook(deleteMessage.Book)
            };

            _proxy.SendWebhook(payload, Settings);
        }

        public override void OnBookFileDelete(BookFileDeleteMessage deleteMessage)
        {
            var payload = new WebhookBookFileDeletePayload
            {
                EventType = WebhookEventType.Delete,
                Author = new WebhookAuthor(deleteMessage.Book.Author),
                Book = new WebhookBook(deleteMessage.Book),
                BookFile = new WebhookBookFile(deleteMessage.BookFile)
            };

            _proxy.SendWebhook(payload, Settings);
        }

        public override void OnBookRetag(BookRetagMessage message)
        {
            var payload = new WebhookRetagPayload
            {
                EventType = WebhookEventType.Retag,
                Author = new WebhookAuthor(message.Author)
            };

            _proxy.SendWebhook(payload, Settings);
        }

        public override void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
            var payload = new WebhookHealthPayload
                          {
                              EventType = WebhookEventType.Health,
                              Level = healthCheck.Type,
                              Message = healthCheck.Message,
                              Type = healthCheck.Source.Name,
                              WikiUrl = healthCheck.WikiUrl?.ToString()
                          };

            _proxy.SendWebhook(payload, Settings);
        }

        public override void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
        {
            var payload = new WebhookApplicationUpdatePayload
            {
                EventType = WebhookEventType.ApplicationUpdate,
                Message = updateMessage.Message,
                PreviousVersion = updateMessage.PreviousVersion.ToString(),
                NewVersion = updateMessage.NewVersion.ToString()
            };

            _proxy.SendWebhook(payload, Settings);
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
                var payload = new WebhookGrabPayload
                {
                    EventType = WebhookEventType.Test,
                    Author = new WebhookAuthor()
                    {
                        Id = 1,
                        Name = "Test Name",
                        Path = "C:\\testpath",
                        MBId = "aaaaa-aaa-aaaa-aaaaaa"
                    },
                    Books = new List<WebhookBook>()
                    {
                            new WebhookBook()
                            {
                                Id = 123,
                                Title = "Test title"
                            }
                    }
                };

                _proxy.SendWebhook(payload, Settings);
            }
            catch (WebhookException ex)
            {
                return new NzbDroneValidationFailure("Url", ex.Message);
            }

            return null;
        }
    }
}
