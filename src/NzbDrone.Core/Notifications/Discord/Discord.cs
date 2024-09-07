using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation.Results;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MediaFiles;
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
            var author = message.Author;
            var authorMetadata = message.Author.Metadata.Value;
            var edition = message.RemoteBook.Books.First().Editions.Value.Single(x => x.Monitored);

            var embed = new Embed
            {
                Author = new DiscordAuthor
                {
                    Name = Settings.Author.IsNullOrWhiteSpace() ? Environment.MachineName : Settings.Author,
                    IconUrl = "https://raw.githubusercontent.com/readarr/Readarr/develop/Logo/256.png"
                },
                Url = $"https://www.goodreads.com/author/show/{author.ForeignAuthorId}",
                Description = "Book Grabbed",
                Title = GetTitle(message.Author, message.RemoteBook.Books),
                Color = (int)DiscordColors.Standard,
                Fields = new List<DiscordField>(),
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };

            if (Settings.GrabFields.Contains((int)DiscordGrabFieldType.Cover))
            {
                embed.Image = new DiscordImage
                {
                    Url = edition.Images.FirstOrDefault(c => c.CoverType == MediaCoverTypes.Cover)?.Url
                };
            }

            if (Settings.GrabFields.Contains((int)DiscordGrabFieldType.Poster))
            {
                embed.Thumbnail = new DiscordImage
                {
                    Url = authorMetadata.Images.FirstOrDefault(x => x.CoverType == MediaCoverTypes.Poster)?.Url
                };
            }

            foreach (var field in Settings.GrabFields)
            {
                var discordField = new DiscordField();

                switch ((DiscordGrabFieldType)field)
                {
                    case DiscordGrabFieldType.Overview:
                        var overview = edition.Overview ?? "";
                        discordField.Name = "Overview";
                        discordField.Value = overview.Length <= 300 ? overview : $"{overview.AsSpan(0, 300)}...";
                        break;
                    case DiscordGrabFieldType.Rating:
                        discordField.Name = "Rating";
                        discordField.Value = message.RemoteBook.Books.First().Ratings.Value.ToString();
                        break;
                    case DiscordGrabFieldType.Genres:
                        discordField.Name = "Genres";
                        discordField.Value = message.RemoteBook.Books.First().Genres.Take(5).Join(", ");
                        break;
                    case DiscordGrabFieldType.Quality:
                        discordField.Name = "Quality";
                        discordField.Inline = true;
                        discordField.Value = message.Quality.Quality.Name;
                        break;
                    case DiscordGrabFieldType.Group:
                        discordField.Name = "Group";
                        discordField.Value = message.RemoteBook.ParsedBookInfo.ReleaseGroup;
                        break;
                    case DiscordGrabFieldType.Size:
                        discordField.Name = "Size";
                        discordField.Value = BytesToString(message.RemoteBook.Release.Size);
                        discordField.Inline = true;
                        break;
                    case DiscordGrabFieldType.Release:
                        discordField.Name = "Release";
                        discordField.Value = string.Format("```{0}```", message.RemoteBook.Release.Title);
                        break;
                    case DiscordGrabFieldType.Links:
                        discordField.Name = "Links";
                        discordField.Value = GetLinksString(edition);
                        break;
                    case DiscordGrabFieldType.CustomFormats:
                        discordField.Name = "Custom Formats";
                        discordField.Value = string.Join("|", message.RemoteBook.CustomFormats);
                        break;
                    case DiscordGrabFieldType.CustomFormatScore:
                        discordField.Name = "Custom Format Score";
                        discordField.Value = message.RemoteBook.CustomFormatScore.ToString();
                        break;
                    case DiscordGrabFieldType.Indexer:
                        discordField.Name = "Indexer";
                        discordField.Value = message.RemoteBook.Release.Indexer;
                        break;
                }

                if (discordField.Name.IsNotNullOrWhiteSpace() && discordField.Value.IsNotNullOrWhiteSpace())
                {
                    embed.Fields.Add(discordField);
                }
            }

            var payload = CreatePayload(null, new List<Embed> { embed });

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnReleaseImport(BookDownloadMessage message)
        {
            var author = message.Author;
            var authorMetadata = message.Author.Metadata.Value;
            var edition = message.BookFiles.First().Edition.Value;
            var isUpgrade = message.OldFiles.Count > 0;

            var embed = new Embed
            {
                Author = new DiscordAuthor
                {
                    Name = Settings.Author.IsNullOrWhiteSpace() ? Environment.MachineName : Settings.Author,
                    IconUrl = "https://raw.githubusercontent.com/readarr/Readarr/develop/Logo/256.png"
                },
                Url = $"https://www.goodreads.com/author/show/{author.ForeignAuthorId}",
                Description = isUpgrade ? "Book Upgraded" : "Book Imported",
                Title = GetTitle(message.Author, new List<Book> { message.Book }),
                Color = isUpgrade ? (int)DiscordColors.Upgrade : (int)DiscordColors.Success,
                Fields = new List<DiscordField>(),
                Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };

            if (Settings.GrabFields.Contains((int)DiscordGrabFieldType.Cover))
            {
                embed.Image = new DiscordImage
                {
                    Url = edition.Images.FirstOrDefault(c => c.CoverType == MediaCoverTypes.Cover)?.Url
                };
            }

            if (Settings.GrabFields.Contains((int)DiscordGrabFieldType.Poster))
            {
                embed.Thumbnail = new DiscordImage
                {
                    Url = authorMetadata.Images.FirstOrDefault(x => x.CoverType == MediaCoverTypes.Poster)?.Url
                };
            }

            foreach (var field in Settings.ImportFields)
            {
                var discordField = new DiscordField();

                switch ((DiscordImportFieldType)field)
                {
                    case DiscordImportFieldType.Overview:
                        var overview = edition.Overview ?? "";
                        discordField.Name = "Overview";
                        discordField.Value = overview.Length <= 300 ? overview : overview.Substring(0, 300) + "...";
                        break;
                    case DiscordImportFieldType.Rating:
                        discordField.Name = "Rating";
                        discordField.Value = message.Book.Ratings.Value.ToString();
                        break;
                    case DiscordImportFieldType.Genres:
                        discordField.Name = "Genres";
                        discordField.Value = message.Book.Genres.Take(5).Join(", ");
                        break;
                    case DiscordImportFieldType.Quality:
                        discordField.Name = "Quality";
                        discordField.Inline = true;
                        discordField.Value = message.BookFiles.First().Quality.Quality.Name;
                        break;
                    case DiscordImportFieldType.Group:
                        discordField.Name = "Group";
                        discordField.Value = message.BookFiles.First().ReleaseGroup;
                        break;
                    case DiscordImportFieldType.Size:
                        discordField.Name = "Size";
                        discordField.Value = BytesToString(message.BookFiles.Sum(x => x.Size));
                        discordField.Inline = true;
                        break;
                    case DiscordImportFieldType.Release:
                        discordField.Name = "Release";
                        discordField.Value = message.BookFiles.First().SceneName;
                        break;
                    case DiscordImportFieldType.Links:
                        discordField.Name = "Links";
                        discordField.Value = GetLinksString(edition);
                        break;
                }

                if (discordField.Name.IsNotNullOrWhiteSpace() && discordField.Value.IsNotNullOrWhiteSpace())
                {
                    embed.Fields.Add(discordField);
                }
            }

            var payload = CreatePayload(null, new List<Embed> { embed });

            _proxy.SendPayload(payload, Settings);
        }

        public override void OnRename(Author author, List<RenamedBookFile> renamedFiles)
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

        public override void OnAuthorAdded(Author author)
        {
            var attachments = new List<Embed>
                              {
                                  new Embed
                                  {
                                      Title = author.Name,
                                      Fields = new List<DiscordField>()
                                      {
                                          new DiscordField()
                                          {
                                              Name = "Links",
                                              Value = string.Join(" / ", author.Metadata.Value.Links.Select(link => $"[{link.Name}]({link.Url})"))
                                          }
                                      },
                                  }
                              };
            var payload = CreatePayload($"Author Added", attachments);

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
                                      Author = new DiscordAuthor
                                      {
                                          Name = Settings.Author.IsNullOrWhiteSpace() ? Environment.MachineName : Settings.Author,
                                          IconUrl = "https://raw.githubusercontent.com/readarr/Readarr/develop/Logo/256.png"
                                      },
                                      Title = healthCheck.Source.Name,
                                      Description = healthCheck.Message,
                                      Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                                      Color = healthCheck.Type == HealthCheck.HealthCheckResult.Warning ? (int)DiscordColors.Warning : (int)DiscordColors.Danger
                                  }
                              };

            var payload = CreatePayload(null, attachments);

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

        private string BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            if (byteCount == 0)
            {
                return "0 " + suf[0];
            }

            var bytes = Math.Abs(byteCount);
            var place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            var num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return string.Format("{0} {1}", (Math.Sign(byteCount) * num).ToString(), suf[place]);
        }

        private string GetLinksString(Edition edition)
        {
            var links = edition.Links.Select(link => $"[{link.Name}]({link.Url})");
            return string.Join(" / ", links);
        }

        private string GetTitle(Author author, List<Book> books)
        {
            var bookTitles = string.Join(" + ", books.Select(e => e.Title));
            return $"{author.Name} - {bookTitles}";
        }
    }
}
