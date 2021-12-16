using System.Collections.Generic;
using FluentValidation.Results;
using NzbDrone.Common.EnvironmentInfo;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;

namespace NzbDrone.Core.Notifications.Synology
{
    public class SynologyIndexer : NotificationBase<SynologyIndexerSettings>
    {
        private readonly ISynologyIndexerProxy _indexerProxy;

        public SynologyIndexer(ISynologyIndexerProxy indexerProxy)
        {
            _indexerProxy = indexerProxy;
        }

        public override string Link => "https://www.synology.com";
        public override string Name => "Synology Indexer";

        public override void OnReleaseImport(BookDownloadMessage message)
        {
            if (Settings.UpdateLibrary)
            {
                foreach (var oldFile in message.OldFiles)
                {
                    var fullPath = oldFile.Path;

                    _indexerProxy.DeleteFile(fullPath);
                }

                foreach (var newFile in message.BookFiles)
                {
                    var fullPath = newFile.Path;

                    _indexerProxy.AddFile(fullPath);
                }
            }
        }

        public override void OnRename(Author author)
        {
            if (Settings.UpdateLibrary)
            {
                _indexerProxy.UpdateFolder(author.Path);
            }
        }

        public override void OnAuthorDelete(AuthorDeleteMessage deleteMessage)
        {
            if (Settings.UpdateLibrary)
            {
                _indexerProxy.DeleteFolder(deleteMessage.Author.Path);
            }
        }

        public override void OnBookDelete(BookDeleteMessage deleteMessage)
        {
            if (Settings.UpdateLibrary && deleteMessage.DeletedFiles)
            {
                foreach (var bookFile in deleteMessage.Book.BookFiles.Value)
                {
                    _indexerProxy.DeleteFile(bookFile.Path);
                }
            }
        }

        public override void OnBookFileDelete(BookFileDeleteMessage deleteMessage)
        {
            if (Settings.UpdateLibrary)
            {
                _indexerProxy.DeleteFile(deleteMessage.BookFile.Path);
            }
        }

        public override void OnBookRetag(BookRetagMessage message)
        {
            if (Settings.UpdateLibrary)
            {
                _indexerProxy.UpdateFolder(message.Author.Path);
            }
        }

        public override ValidationResult Test()
        {
            var failures = new List<ValidationFailure>();

            failures.AddIfNotNull(TestConnection());

            return new ValidationResult(failures);
        }

        protected virtual ValidationFailure TestConnection()
        {
            if (!OsInfo.IsLinux)
            {
                return new ValidationFailure(null, "Must be a Synology");
            }

            if (!_indexerProxy.Test())
            {
                return new ValidationFailure(null, "Not a Synology or synoindex not available");
            }

            return null;
        }
    }
}
