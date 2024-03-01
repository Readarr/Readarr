using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;
using NzbDrone.Core.Datastore.Events;
using NzbDrone.Core.DecisionEngine.Specifications;
using NzbDrone.Core.Exceptions;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.BookImport.Manual;
using NzbDrone.Core.MediaFiles.Events;
using NzbDrone.Core.Messaging.Events;
using NzbDrone.Http.REST.Attributes;
using NzbDrone.SignalR;
using Readarr.Http;
using Readarr.Http.REST;
using BadRequestException = NzbDrone.Core.Exceptions.BadRequestException;
using HttpStatusCode = System.Net.HttpStatusCode;

namespace Readarr.Api.V1.BookFiles
{
    [V1ApiController]
    public class BookFileController : RestControllerWithSignalR<BookFileResource, BookFile>,
                                 IHandle<BookFileAddedEvent>,
                                 IHandle<BookFileDeletedEvent>
    {
        private readonly IMediaFileService _mediaFileService;
        private readonly IDeleteMediaFiles _mediaFileDeletionService;
        private readonly IMetadataTagService _metadataTagService;
        private readonly IAuthorService _authorService;
        private readonly IBookService _bookService;
        private readonly IUpgradableSpecification _upgradableSpecification;
        private readonly IContentTypeProvider _mimeTypeProvider;
        private readonly IDiskProvider _diskProvider;
        private readonly IManualImportService _manualImportService;

        public BookFileController(IManualImportService manualImportService,
                               IDiskProvider diskProvider,
                               IBroadcastSignalRMessage signalRBroadcaster,
                               IMediaFileService mediaFileService,
                               IDeleteMediaFiles mediaFileDeletionService,
                               IMetadataTagService metadataTagService,
                               IAuthorService authorService,
                               IBookService bookService,
                               IUpgradableSpecification upgradableSpecification)
            : base(signalRBroadcaster)
        {
            _diskProvider = diskProvider;
            _manualImportService = manualImportService;
            _mediaFileService = mediaFileService;
            _mediaFileDeletionService = mediaFileDeletionService;
            _metadataTagService = metadataTagService;
            _authorService = authorService;
            _bookService = bookService;
            _upgradableSpecification = upgradableSpecification;
            _mimeTypeProvider = new FileExtensionContentTypeProvider();
        }

        private BookFileResource MapToResource(BookFile bookFile)
        {
            if (bookFile.EditionId > 0 && bookFile.Author != null && bookFile.Author.Value != null)
            {
                return bookFile.ToResource(bookFile.Author.Value, _upgradableSpecification);
            }
            else
            {
                return bookFile.ToResource();
            }
        }

        protected override BookFileResource GetResourceById(int id)
        {
            var resource = MapToResource(_mediaFileService.Get(id));
            resource.AudioTags = _metadataTagService.ReadTags((FileInfoBase)new FileInfo(resource.Path));
            return resource;
        }

        [HttpGet]
        public List<BookFileResource> GetBookFiles(int? authorId, [FromQuery]List<int> bookFileIds, [FromQuery(Name="bookId")]List<int> bookIds, bool? unmapped)
        {
            if (!authorId.HasValue && !bookFileIds.Any() && !bookIds.Any() && !unmapped.HasValue)
            {
                throw new BadRequestException("authorId, bookId, bookFileIds or unmapped must be provided");
            }

            if (unmapped.HasValue && unmapped.Value)
            {
                var files = _mediaFileService.GetUnmappedFiles();
                return files.ConvertAll(f => MapToResource(f));
            }

            if (authorId.HasValue && !bookIds.Any())
            {
                var author = _authorService.GetAuthor(authorId.Value);

                return _mediaFileService.GetFilesByAuthor(authorId.Value).ConvertAll(f => f.ToResource(author, _upgradableSpecification));
            }

            if (bookIds.Any())
            {
                var result = new List<BookFileResource>();
                foreach (var bookId in bookIds)
                {
                    var book = _bookService.GetBook(bookId);
                    var bookAuthor = _authorService.GetAuthor(book.AuthorId);
                    result.AddRange(_mediaFileService.GetFilesByBook(book.Id).ConvertAll(f => f.ToResource(bookAuthor, _upgradableSpecification)));
                }

                return result;
            }
            else
            {
                // trackfiles will come back with the author already populated
                var bookFiles = _mediaFileService.Get(bookFileIds);
                return bookFiles.ConvertAll(e => MapToResource(e));
            }
        }

        [HttpGet("download/{id:int}")]
        public IActionResult GetBookFile(int id)
        {
            try
            {
                var bookFile = _mediaFileService.Get(id);
                var filePath = bookFile.Path;
                Response.Headers.Add("content-disposition", string.Format("attachment;filename={0}", PathExtensions.BaseName(filePath)));
                return new PhysicalFileResult(filePath, GetContentType(filePath));
            }
            catch
            {
                throw new BadRequestException(string.Format("no bookfiles exist for id: {0}", id));
            }
        }

        [HttpPost("upload/{id:int}")]
        public async Task<IActionResult> PutBookFileAsync(int id, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new NzbDroneClientException(HttpStatusCode.UnprocessableEntity, "no file selected for upload");
            }
            else if (file.Length > 2.5e+7)
            {
                throw new NzbDroneClientException(HttpStatusCode.UnprocessableEntity, "file is too large for upload. max file size is 25 MB.");
            }

            var contentType = file.ContentType;
            var book = _bookService.GetBook(id);
            var title = book.Title;
            var bookAuthor = _authorService.GetAuthor(book.AuthorId);
            var extension = GetExtension(Path.GetFileName(file.FileName));
            if (!MediaFileExtensions.AllExtensions.Contains(extension))
            {
                throw new NzbDroneClientException(HttpStatusCode.UnprocessableEntity, "invalid content type for upload.");
            }

            //create a tmpdirectory with the given id
            var directory = Path.Join(Path.GetTempPath(), book.Title);
            if (!_diskProvider.FolderExists(directory))
            {
                _diskProvider.CreateFolder(directory);
            }

            //don't use the uploaded file's name in case it is intentionally malformed
            var fileName = string.Format("{0}{1}", title, extension);
            var combined = Path.Combine(directory, fileName);

            using (var fileStream = new FileStream(combined, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            var list = _manualImportService.ProcessFile(combined, book, bookAuthor, FilterFilesType.None, false);
            if (list.Empty())
            {
                //delete the directory after manual import
                _diskProvider.DeleteFolder(directory, true);
                throw new NzbDroneClientException(HttpStatusCode.UnprocessableEntity, "import failed.");
            }
            else if (!list.First().Rejections.Empty())
            {
                //delete the directory after manual import
                _diskProvider.DeleteFolder(directory, true);
                throw new NzbDroneClientException(HttpStatusCode.UnprocessableEntity, "import failed.");
            }

            //delete the directory after manual import
            _diskProvider.DeleteFolder(directory, true);
            return Accepted();
        }

        [RestPutById]
        public ActionResult<BookFileResource> SetQuality(BookFileResource bookFileResource)
        {
            var bookFile = _mediaFileService.Get(bookFileResource.Id);
            bookFile.Quality = bookFileResource.Quality;
            _mediaFileService.Update(bookFile);
            return Accepted(bookFile.Id);
        }

        [HttpPut("editor")]
        public IActionResult SetQuality([FromBody] BookFileListResource resource)
        {
            var bookFiles = _mediaFileService.Get(resource.BookFileIds);

            foreach (var bookFile in bookFiles)
            {
                if (resource.Quality != null)
                {
                    bookFile.Quality = resource.Quality;
                }
            }

            _mediaFileService.Update(bookFiles);

            return Accepted(bookFiles.ConvertAll(f => f.ToResource(bookFiles.First().Author.Value, _upgradableSpecification)));
        }

        [RestDeleteById]
        public void DeleteBookFile(int id)
        {
            var bookFile = _mediaFileService.Get(id);

            if (bookFile == null)
            {
                throw new NzbDroneClientException(HttpStatusCode.NotFound, "Book file not found");
            }

            if (bookFile.EditionId > 0 && bookFile.Author != null && bookFile.Author.Value != null)
            {
                _mediaFileDeletionService.DeleteTrackFile(bookFile.Author.Value, bookFile);
            }
            else
            {
                _mediaFileDeletionService.DeleteTrackFile(bookFile, "Unmapped_Files");
            }
        }

        [HttpDelete("bulk")]
        public object DeleteTrackFiles([FromBody] BookFileListResource resource)
        {
            var bookFiles = _mediaFileService.Get(resource.BookFileIds);

            foreach (var bookFile in bookFiles)
            {
                if (bookFile.EditionId > 0 && bookFile.Author != null && bookFile.Author.Value != null)
                {
                    _mediaFileDeletionService.DeleteTrackFile(bookFile.Author.Value, bookFile);
                }
                else
                {
                    _mediaFileDeletionService.DeleteTrackFile(bookFile, "Unmapped_Files");
                }
            }

            return new { };
        }

        [NonAction]
        public void Handle(BookFileAddedEvent message)
        {
            BroadcastResourceChange(ModelAction.Updated, MapToResource(message.BookFile));
        }

        [NonAction]
        public void Handle(BookFileDeletedEvent message)
        {
            BroadcastResourceChange(ModelAction.Deleted, MapToResource(message.BookFile));
        }

        private string GetContentType(string filePath)
        {
            if (!_mimeTypeProvider.TryGetContentType(filePath, out var contentType))
            {
                var ext = PathExtensions.GetPathExtension(filePath);
                if (ext.Contains("epub"))
                {
                    contentType = "application/epub+zip";
                }
                else if (ext.Contains("azw"))
                {
                    contentType = "application/vnd.amazon.ebook";
                }
                else if (ext.Contains("azw"))
                {
                    contentType = "application/x-mobipocket-ebook";
                }
                else if (ext.Contains("pdf"))
                {
                    contentType = "application/pdf";
                }
                else
                {
                    contentType = "application/octet-stream";
                }
            }

            return contentType;
        }

        private static string GetExtension(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            var index = path.LastIndexOf('.');
            if (index < 0)
            {
                return null;
            }

            return path.Substring(index);
        }
    }
}
