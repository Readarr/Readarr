using System;
using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;
using NzbDrone.Core.Notifications.Discord.Payloads;
using NzbDrone.Core.Validation;

namespace NzbDrone.Core.Notifications.Discord
{
    public class Discord : NotificationBase<DiscordSettings>
    {
        private readonly IDiscordProxy _proxy;

        public Discord(IDiscordProxy proxy)
        {
            _proxy = proxy;
        }

        public override string Name => "Discord";
        public override string Link => "https://support.discordapp.com/hc/en-us/articles/228383668-Intro-to-Webhooks";

        public override void OnGrab(GrabMessage message)
        {
            var embeds = new List<Embed>
                              {
                                  new Embed
                                  {
                                      Description = message.Message,
                                      Title = message.Author.Name,
                                      Text = message.Message,
                                      Color = (int)DiscordColors.Warning
                                  }
                              };
            var payload = CreatePayload($"Grabbed: {message.Message}", embeds);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnReleaseImport(BookDownloadMessage message)
        {
            var attachments = new List<Embed>
            {
                new Embed
                {
                    Description = message.Message,
                    Title = message.Author.Name,
                    Text = message.Message,
                    Color = (int)DiscordColors.Success
                }
            };
            var payload = CreatePayload($"Imported: {message.Message}", attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnRename(Author author)
        {
            var attachments = new List<Embed>
                              {
                                  new Embed
                                  {
                                      Title = author.Name,
                                  }
                              };

            var payload = CreatePayload("Renamed", attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnAuthorDelete(AuthorDeleteMessage deleteMessage)
        {
            var attachments = new List<Embed>
                              {
                                  new Embed
                                  {
                                      Title = deleteMessage.Author.Name,
                                      Description = deleteMessage.DeletedFilesMessage
                                  }
                              };

            var payload = CreatePayload("Author Deleted", attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnBookDelete(BookDeleteMessage deleteMessage)
        {
            var attachments = new List<Embed>
                              {
                                  new Embed
                                  {
                                    Title = $"${deleteMessage.Book.Author.Value.Name} - ${deleteMessage.Book.Title}",
                                    Description = deleteMessage.DeletedFilesMessage
                                  }
                              };

            var payload = CreatePayload("Book Deleted", attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnBookFileDelete(BookFileDeleteMessage deleteMessage)
        {
            var attachments = new List<Embed>
                              {
                                  new Embed
                                  {
                                    Title = $"${deleteMessage.Book.Author.Value.Name} - ${deleteMessage.Book.Title} - file deleted",
                                    Description = deleteMessage.BookFile.Path
                                  }
                              };

            var payload = CreatePayload("Book File Deleted", attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnHealthIssue(HealthCheck.HealthCheck healthCheck)
        {
            var attachments = new List<Embed>
                              {
                                  new Embed
                                  {
                                      Title = healthCheck.Source.Name,
                                      Text = healthCheck.Message,
                                      Color = healthCheck.Type == HealthCheck.HealthCheckResult.Warning ? (int)DiscordColors.Warning : (int)DiscordColors.Danger
                                  }
                              };

            var payload = CreatePayload("Health Issue", attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnBookRetag(BookRetagMessage message)
        {
            var attachments = new List<Embed>
                              {
                                  new Embed
                                  {
                                      Title = BOOK_RETAGGED_TITLE,
                                      Text = message.Message
                                  }
                              };

            var payload = CreatePayload($"Track file tags updated: {message.Message}", attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnDownloadFailure(DownloadFailedMessage message)
        {
            var attachments = new List<Embed>
            {
                new Embed
                {
                    Description = message.Message,
                    Title = message.SourceTitle,
                    Text = message.Message,
                    Color = (int)DiscordColors.Danger
                }
            };
            var payload = CreatePayload($"Download Failed: {message.Message}", attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnImportFailure(BookDownloadMessage message)
        {
            var attachments = new List<Embed>
            {
                new Embed
                {
                    Description = message.Message,
                    Title = message.Book?.Title ?? message.Message,
                    Text = message.Message,
                    Color = (int)DiscordColors.Warning
                }
            };
            var payload = CreatePayload($"Import Failed: {message.Message}", attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnApplicationUpdate(ApplicationUpdateMessage updateMessage)
        {
            var attachments = new List<Embed>
                              {
                                  new Embed
                                  {
                                      Author = new DiscordAuthor
                                      {
                                          Name = Settings.Author.IsNullOrWhiteSpace() ? Environment.MachineName : Settings.Author,
                                          IconUrl = "https://raw.githubusercontent.com/Readarr/Readarr/develop/Logo/256.png"
                                      },
                                      Title = APPLICATION_UPDATE_TITLE,
                                      Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                                      Color = (int)DiscordColors.Standard,
                                      Fields = new List<DiscordField>()
                                      {
                                          new DiscordField()
                                          {
                                              Name = "Previous Version",
                                              Value = updateMessage.PreviousVersion.ToString()
                                          },
                                          new DiscordField()
                                          {
                                              Name = "New Version",
                                              Value = updateMessage.NewVersion.ToString()
                                          }
                                      },
                                  }
                              };

            var payload = CreatePayload(null, attachments);

            _proxy.SendPayload(payload, Settings);
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(TestMessage());

            return new ValidationResult(failures);
        }

        public ValidationFailure TestMessage()
        {
            try
            {
                var message = $"Test message from Readarr posted at {DateTime.Now}";
                var payload = CreatePayload(message);

                _proxy.SendPayload(payload, Settings);
            }
            catch (DiscordException ex)
            {
                return new NzbDroneValidationFailure("Unable to post", ex.Message);
            }

            return null;
        }

        private DiscordPayload CreatePayload(string message, List<Embed> embeds = null)
        {
            var avatar = Settings.Avatar;

            var payload = new DiscordPayload
            {
                Username = Settings.Username,
                Content = message,
                Embeds = embeds
            };

            if (avatar.IsNotNullOrWhiteSpace())
            {
                payload.AvatarUrl = avatar;
            }

            if (Settings.Username.IsNotNullOrWhiteSpace())
            {
                payload.Username = Settings.Username;
            }

            return payload;
        }
    }
}
