using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using NLog;
using NzbDrone.Core.Books;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles
{
    public interface IMetadataTagService
    {
        ParsedTrackInfo ReadTags(IFileInfo file);
        void WriteTags(BookFile trackfile, bool newDownload, bool force = false);
        void SyncTags(List<Edition> books);
        List<RetagBookFilePreview> GetRetagPreviewsByAuthor(int authorId);
        List<RetagBookFilePreview> GetRetagPreviewsByBook(int authorId);
    }

    public class MetadataTagService : IMetadataTagService,
        IExecute<RetagFilesCommand>,
        IExecute<RetagAuthorCommand>
    {
        private readonly IAudioTagService _audioTagService;
        private readonly IEBookTagService _eBookTagService;
        private readonly Logger _logger;

        public MetadataTagService(IAudioTagService audioTagService,
            IEBookTagService eBookTagService,
            Logger logger)
        {
            _audioTagService = audioTagService;
            _eBookTagService = eBookTagService;

            _logger = logger;
        }

        public ParsedTrackInfo ReadTags(IFileInfo file)
        {
            if (MediaFileExtensions.AudioExtensions.Contains(file.Extension))
            {
                return _audioTagService.ReadTags(file.FullName);
            }
            else
            {
                return _eBookTagService.ReadTags(file);
            }
        }

        public void WriteTags(BookFile bookFile, bool newDownload, bool force = false)
        {
            var extension = Path.GetExtension(bookFile.Path);
            if (MediaFileExtensions.AudioExtensions.Contains(extension))
            {
                _audioTagService.WriteTags(bookFile, newDownload, force);
            }
            else if (bookFile.CalibreId > 0)
            {
                _eBookTagService.WriteTags(bookFile, newDownload, force);
            }
        }

        public void SyncTags(List<Edition> editions)
        {
            _audioTagService.SyncTags(editions);
            _eBookTagService.SyncTags(editions);
        }

        public List<RetagBookFilePreview> GetRetagPreviewsByAuthor(int authorId)
        {
            var previews = _audioTagService.GetRetagPreviewsByAuthor(authorId);
            previews.AddRange(_eBookTagService.GetRetagPreviewsByAuthor(authorId));

            return previews;
        }

        public List<RetagBookFilePreview> GetRetagPreviewsByBook(int bookId)
        {
            var previews = _audioTagService.GetRetagPreviewsByBook(bookId);
            previews.AddRange(_eBookTagService.GetRetagPreviewsByBook(bookId));

            return previews;
        }

        public void Execute(RetagFilesCommand message)
        {
            _eBookTagService.RetagFiles(message);
            _audioTagService.RetagFiles(message);
        }

        public void Execute(RetagAuthorCommand message)
        {
            _eBookTagService.RetagAuthor(message);
            _audioTagService.RetagAuthor(message);
        }
    }
}
