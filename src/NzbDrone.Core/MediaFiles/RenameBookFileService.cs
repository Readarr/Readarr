using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Books;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Core.Organizer;

namespace NzbDrone.Core.MediaFiles
{
    public interface IRenameBookFileService
    {
        List<RenameBookFilePreview> GetRenamePreviews(int authorId);
        List<RenameBookFilePreview> GetRenamePreviews(int authorId, int bookId);
    }

    public class RenameBookFileService : IRenameBookFileService, IExecute<RenameFilesCommand>, IExecute<RenameAuthorCommand>
    {
        private readonly IAuthorService _authorService;
        private readonly IMediaFileService _mediaFileService;
        private readonly IMoveBookFiles _bookFileMover;
        private readonly IEventAggregator _eventAggregator;
        private readonly IBuildFileNames _filenameBuilder;
        private readonly IDiskProvider _diskProvider;
        private readonly Logger _logger;

        public RenameBookFileService(IAuthorService authorService,
                                        IMediaFileService mediaFileService,
                                        IMoveBookFiles bookFileMover,
                                        IEventAggregator eventAggregator,
                                        IBuildFileNames filenameBuilder,
                                        IDiskProvider diskProvider,
                                        Logger logger)
        {
            _authorService = authorService;
            _mediaFileService = mediaFileService;
            _bookFileMover = bookFileMover;
            _eventAggregator = eventAggregator;
            _filenameBuilder = filenameBuilder;
            _diskProvider = diskProvider;
            _logger = logger;
        }

        public List<RenameBookFilePreview> GetRenamePreviews(int authorId)
        {
            var author = _authorService.GetAuthor(authorId);
            var files = _mediaFileService.GetFilesByAuthor(authorId);

            _logger.Trace($"got {files.Count} files");

            return GetPreviews(author, files)
                .OrderByDescending(e => e.BookId)
                .ThenBy(e => e.ExistingPath)
                .ToList();
        }

        public List<RenameBookFilePreview> GetRenamePreviews(int authorId, int bookId)
        {
            var author = _authorService.GetAuthor(authorId);
            var files = _mediaFileService.GetFilesByBook(bookId);

            return GetPreviews(author, files)
                .OrderBy(e => e.ExistingPath).ToList();
        }

        private IEnumerable<RenameBookFilePreview> GetPreviews(Author author, List<BookFile> files)
        {
            var counts = files.GroupBy(x => x.EditionId).ToDictionary(g => g.Key, g => g.Count());

            // Don't rename Calibre files
            foreach (var f in files.Where(x => x.CalibreId == 0))
            {
                var file = f;
                file.PartCount = counts[file.EditionId];

                var book = file.Edition.Value;
                var bookFilePath = file.Path;

                if (book == null)
                {
                    _logger.Warn("File ({0}) is not linked to a book", bookFilePath);
                    continue;
                }

                var newName = _filenameBuilder.BuildBookFileName(author, book, file);

                _logger.Trace($"got name {newName}");

                var newPath = _filenameBuilder.BuildBookFilePath(author, book, newName, Path.GetExtension(bookFilePath));

                _logger.Trace($"got path {newPath}");

                if (!bookFilePath.PathEquals(newPath, StringComparison.Ordinal))
                {
                    yield return new RenameBookFilePreview
                    {
                        AuthorId = author.Id,
                        BookId = book.Id,
                        BookFileId = file.Id,
                        ExistingPath = file.Path,
                        NewPath = newPath
                    };
                }
            }
        }

        private void RenameFiles(List<BookFile> bookFiles, Author author)
        {
            var allFiles = _mediaFileService.GetFilesByAuthor(author.Id);
            var counts = allFiles.GroupBy(x => x.EditionId).ToDictionary(g => g.Key, g => g.Count());
            var renamed = new List<RenamedBookFile>();

            // Don't rename Calibre files
            foreach (var bookFile in bookFiles.Where(x => x.CalibreId == 0))
            {
                var previousPath = bookFile.Path;
                bookFile.PartCount = counts[bookFile.EditionId];

                try
                {
                    _logger.Debug("Renaming book file: {0}", bookFile);
                    _bookFileMover.MoveBookFile(bookFile, author);

                    _mediaFileService.Update(bookFile);

                    renamed.Add(new RenamedBookFile
                    {
                        BookFile = bookFile,
                        PreviousPath = previousPath
                    });

                    _logger.Debug("Renamed book file: {0}", bookFile);

                    _eventAggregator.PublishEvent(new BookFileRenamedEvent(author, bookFile, previousPath));
                }
                catch (FileAlreadyExistsException ex)
                {
                    _logger.Warn("File not renamed, there is already a file at the destination: {0}", ex.Filename);
                }
                catch (SameFilenameException ex)
                {
                    _logger.Debug("File not renamed, source and destination are the same: {0}", ex.Filename);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed to rename file {0}", previousPath);
                }
            }

            if (renamed.Any())
            {
                _eventAggregator.PublishEvent(new AuthorRenamedEvent(author, renamed));

                _logger.Debug("Removing Empty Subfolders from: {0}", author.Path);
                _diskProvider.RemoveEmptySubfolders(author.Path);
            }
        }

        public void Execute(RenameFilesCommand message)
        {
            var author = _authorService.GetAuthor(message.AuthorId);
            var bookFiles = _mediaFileService.Get(message.Files);

            _logger.ProgressInfo("Renaming {0} files for {1}", bookFiles.Count, author.Name);
            RenameFiles(bookFiles, author);
            _logger.ProgressInfo("Selected book files renamed for {0}", author.Name);
        }

        public void Execute(RenameAuthorCommand message)
        {
            _logger.Debug("Renaming all files for selected author");
            var authorToRename = _authorService.GetAuthors(message.AuthorIds);

            foreach (var author in authorToRename)
            {
                var bookFiles = _mediaFileService.GetFilesByAuthor(author.Id);
                _logger.ProgressInfo("Renaming all files in author: {0}", author.Name);
                RenameFiles(bookFiles, author);
                _logger.ProgressInfo("All book files renamed for {0}", author.Name);
            }
        }
    }
}
