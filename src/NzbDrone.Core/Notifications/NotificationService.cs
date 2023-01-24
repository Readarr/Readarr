using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;
using NzbDrone.Core.Books.Events;
using NzbDrone.Core.Download;
using NzbDrone.Core.HealthCheck;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.ThingiProvider;
using NzbDrone.Core.Update.History.Events;

namespace NzbDrone.Core.Notifications
{
    public class NotificationService
        : IHandle<BookGrabbedEvent>,
          IHandle<BookImportedEvent>,
          IHandle<AuthorRenamedEvent>,
          IHandle<AuthorDeletedEvent>,
          IHandle<BookDeletedEvent>,
          IHandle<BookFileDeletedEvent>,
          IHandle<HealthCheckFailedEvent>,
          IHandle<DownloadFailedEvent>,
          IHandle<BookImportIncompleteEvent>,
          IHandle<BookFileRetaggedEvent>,
          IHandleAsync<DeleteCompletedEvent>,
          IHandle<UpdateInstalledEvent>
    {
        private readonly INotificationFactory _notificationFactory;
        private readonly Logger _logger;

        public NotificationService(INotificationFactory notificationFactory, Logger logger)
        {
            _notificationFactory = notificationFactory;
            _logger = logger;
        }

        private string GetMessage(Author author, List<Book> books, QualityModel quality)
        {
            var qualityString = quality.Quality.ToString();

            if (quality.Revision.Version > 1)
            {
                qualityString += " Proper";
            }

            var bookTitles = string.Join(" + ", books.Select(e => e.Title));

            return string.Format("{0} - {1} - [{2}]",
                                    author.Name,
                                    bookTitles,
                                    qualityString);
        }

        private string GetBookDownloadMessage(Author author, Book book, List<BookFile> tracks)
        {
            return string.Format("{0} - {1} ({2} Files Imported)",
                author.Name,
                book.Title,
                tracks.Count);
        }

        private string GetBookIncompleteImportMessage(string source)
        {
            return string.Format("Readarr failed to Import all files for {0}",
                source);
        }

        private string FormatMissing(object value)
        {
            var text = value?.ToString();
            return text.IsNullOrWhiteSpace() ? "<missing>" : text;
        }

        private string GetTrackRetagMessage(Author author, BookFile bookFile, Dictionary<string, Tuple<string, string>> diff)
        {
            return string.Format("{0}:\n{1}",
                                 bookFile.Path,
                                 string.Join("\n", diff.Select(x => $"{x.Key}: {FormatMissing(x.Value.Item1)} → {FormatMissing(x.Value.Item2)}")));
        }

        private bool ShouldHandleAuthor(ProviderDefinition definition, Author author)
        {
            if (definition.Tags.Empty())
            {
                _logger.Debug("No tags set for this notification.");
                return true;
            }

            if (definition.Tags.Intersect(author.Tags).Any())
            {
                _logger.Debug("Notification and author have one or more intersecting tags.");
                return true;
            }

            //TODO: this message could be more clear
            _logger.Debug("{0} does not have any intersecting tags with {1}. Notification will not be sent.", definition.Name, author.Name);
            return false;
        }

        private bool ShouldHandleHealthFailure(HealthCheck.HealthCheck healthCheck, bool includeWarnings)
        {
            if (healthCheck.Type == HealthCheckResult.Error)
            {
                return true;
            }

            if (healthCheck.Type == HealthCheckResult.Warning && includeWarnings)
            {
                return true;
            }

            return false;
        }

        public void Handle(BookGrabbedEvent message)
        {
            var grabMessage = new GrabMessage
            {
                Message = GetMessage(message.Book.Author, message.Book.Books, message.Book.ParsedBookInfo.Quality),
                Author = message.Book.Author,
                Quality = message.Book.ParsedBookInfo.Quality,
                Book = message.Book,
                DownloadClient = message.DownloadClient,
                DownloadId = message.DownloadId
            };

            foreach (var notification in _notificationFactory.OnGrabEnabled())
            {
                try
                {
                    if (!ShouldHandleAuthor(notification.Definition, message.Book.Author))
                    {
                        continue;
                    }

                    notification.OnGrab(grabMessage);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Unable to send OnGrab notification to {0}", notification.Definition.Name);
                }
            }
        }

        public void Handle(BookImportedEvent message)
        {
            if (!message.NewDownload)
            {
                return;
            }

            var downloadMessage = new BookDownloadMessage
            {
                Message = GetBookDownloadMessage(message.Author, message.Book, message.ImportedBooks),
                Author = message.Author,
                Book = message.Book,
                DownloadClient = message.DownloadClientInfo?.Name,
                DownloadId = message.DownloadId,
                BookFiles = message.ImportedBooks,
                OldFiles = message.OldFiles,
            };

            foreach (var notification in _notificationFactory.OnReleaseImportEnabled())
            {
                try
                {
                    if (ShouldHandleAuthor(notification.Definition, message.Author))
                    {
                        if (downloadMessage.OldFiles.Empty() || ((NotificationDefinition)notification.Definition).OnUpgrade)
                        {
                            notification.OnReleaseImport(downloadMessage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Unable to send OnReleaseImport notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(AuthorRenamedEvent message)
        {
            foreach (var notification in _notificationFactory.OnRenameEnabled())
            {
                try
                {
                    if (ShouldHandleAuthor(notification.Definition, message.Author))
                    {
                        notification.OnRename(message.Author);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Unable to send OnRename notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(AuthorDeletedEvent message)
        {
            var deleteMessage = new AuthorDeleteMessage(message.Author, message.DeleteFiles);

            foreach (var notification in _notificationFactory.OnAuthorDeleteEnabled())
            {
                try
                {
                    if (ShouldHandleAuthor(notification.Definition, deleteMessage.Author))
                    {
                        notification.OnAuthorDelete(deleteMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Unable to send OnAuthorDelete notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(BookDeletedEvent message)
        {
            var deleteMessage = new BookDeleteMessage(message.Book, message.DeleteFiles);

            foreach (var notification in _notificationFactory.OnBookDeleteEnabled())
            {
                try
                {
                    if (ShouldHandleAuthor(notification.Definition, deleteMessage.Book.Author))
                    {
                        notification.OnBookDelete(deleteMessage);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Unable to send OnBookDelete notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(BookFileDeletedEvent message)
        {
            var deleteMessage = new BookFileDeleteMessage();

            var book = new List<Book> { message.BookFile.Edition.Value.Book };

            deleteMessage.Message = GetMessage(message.BookFile.Author, book, message.BookFile.Quality);
            deleteMessage.BookFile = message.BookFile;
            deleteMessage.Book = message.BookFile.Edition.Value.Book;
            deleteMessage.Reason = message.Reason;

            foreach (var notification in _notificationFactory.OnBookFileDeleteEnabled())
            {
                try
                {
                    if (message.Reason != MediaFiles.DeleteMediaFileReason.Upgrade || ((NotificationDefinition)notification.Definition).OnBookFileDeleteForUpgrade)
                    {
                        if (ShouldHandleAuthor(notification.Definition, message.BookFile.Author))
                        {
                            notification.OnBookFileDelete(deleteMessage);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Unable to send OnBookFileDelete notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(HealthCheckFailedEvent message)
        {
            foreach (var notification in _notificationFactory.OnHealthIssueEnabled())
            {
                try
                {
                    if (ShouldHandleHealthFailure(message.HealthCheck, ((NotificationDefinition)notification.Definition).IncludeHealthWarnings))
                    {
                        notification.OnHealthIssue(message.HealthCheck);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Unable to send OnHealthIssue notification to: " + notification.Definition.Name);
                }
            }
        }

        public void Handle(DownloadFailedEvent message)
        {
            var downloadFailedMessage = new DownloadFailedMessage
            {
                DownloadId = message.DownloadId,
                DownloadClient = message.DownloadClient,
                Quality = message.Quality,
                SourceTitle = message.SourceTitle,
                Message = message.Message
            };

            foreach (var notification in _notificationFactory.OnDownloadFailureEnabled())
            {
                if (ShouldHandleAuthor(notification.Definition, message.TrackedDownload.RemoteBook.Author))
                {
                    notification.OnDownloadFailure(downloadFailedMessage);
                }
            }
        }

        public void Handle(BookImportIncompleteEvent message)
        {
            // TODO: Build out this message so that we can pass on what failed and what was successful
            var downloadMessage = new BookDownloadMessage
            {
                Message = GetBookIncompleteImportMessage(message.TrackedDownload.DownloadItem.Title)
            };

            foreach (var notification in _notificationFactory.OnImportFailureEnabled())
            {
                if (ShouldHandleAuthor(notification.Definition, message.TrackedDownload.RemoteBook.Author))
                {
                    notification.OnImportFailure(downloadMessage);
                }
            }
        }

        public void Handle(BookFileRetaggedEvent message)
        {
            var retagMessage = new BookRetagMessage
            {
                Message = GetTrackRetagMessage(message.Author, message.BookFile, message.Diff),
                Author = message.Author,
                Book = message.BookFile.Edition.Value.Book.Value,
                BookFile = message.BookFile,
                Diff = message.Diff,
                Scrubbed = message.Scrubbed
            };

            foreach (var notification in _notificationFactory.OnBookRetagEnabled())
            {
                if (ShouldHandleAuthor(notification.Definition, message.Author))
                {
                    notification.OnBookRetag(retagMessage);
                }
            }
        }

        public void Handle(UpdateInstalledEvent message)
        {
            var updateMessage = new ApplicationUpdateMessage();
            updateMessage.Message = $"Readarr updated from {message.PreviousVerison.ToString()} to {message.NewVersion.ToString()}";
            updateMessage.PreviousVersion = message.PreviousVerison;
            updateMessage.NewVersion = message.NewVersion;

            foreach (var notification in _notificationFactory.OnApplicationUpdateEnabled())
            {
                try
                {
                    notification.OnApplicationUpdate(updateMessage);
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Unable to send OnApplicationUpdate notification to: " + notification.Definition.Name);
                }
            }
        }

        public void HandleAsync(DeleteCompletedEvent message)
        {
            ProcessQueue();
        }

        private void ProcessQueue()
        {
            foreach (var notification in _notificationFactory.GetAvailableProviders())
            {
                try
                {
                    notification.ProcessQueue();
                }
                catch (Exception ex)
                {
                    _logger.Warn(ex, "Unable to process notification queue for " + notification.Definition.Name);
                }
            }
        }
    }
}
