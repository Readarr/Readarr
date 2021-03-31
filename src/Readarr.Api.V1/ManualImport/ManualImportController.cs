using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NLog;
using NzbDrone.Core.Books;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.MediaFiles.BookImport.Manual;
using NzbDrone.Core.Qualities;
using Readarr.Http;

namespace Readarr.Api.V1.ManualImport
{
    [V1ApiController]
    public class ManualImportController : Controller
    {
        private readonly IAuthorService _authorService;
        private readonly IBookService _bookService;
        private readonly IEditionService _editionService;
        private readonly IManualImportService _manualImportService;
        private readonly Logger _logger;

        public ManualImportController(IManualImportService manualImportService,
                                  IAuthorService authorService,
                                  IEditionService editionService,
                                  IBookService bookService,
                                  Logger logger)
        {
            _authorService = authorService;
            _bookService = bookService;
            _editionService = editionService;
            _manualImportService = manualImportService;
            _logger = logger;
        }

        [HttpPut]
        public IActionResult UpdateItems(List<ManualImportResource> resource)
        {
            return Accepted(UpdateImportItems(resource));
        }

        [HttpGet]
        public List<ManualImportResource> GetMediaFiles(string folder, string downloadId, int? authorId, bool filterExistingFiles = true, bool replaceExistingFiles = true)
        {
            NzbDrone.Core.Books.Author author = null;

            if (authorId > 0)
            {
                author = _authorService.GetAuthor(authorId.Value);
            }

            var filter = filterExistingFiles ? FilterFilesType.Matched : FilterFilesType.None;

            return _manualImportService.GetMediaFiles(folder, downloadId, author, filter, replaceExistingFiles).ToResource().Select(AddQualityWeight).ToList();
        }

        private ManualImportResource AddQualityWeight(ManualImportResource item)
        {
            if (item.Quality != null)
            {
                item.QualityWeight = Quality.DefaultQualityDefinitions.Single(q => q.Quality == item.Quality.Quality).Weight;
                item.QualityWeight += item.Quality.Revision.Real * 10;
                item.QualityWeight += item.Quality.Revision.Version;
            }

            return item;
        }

        private List<ManualImportResource> UpdateImportItems(List<ManualImportResource> resources)
        {
            var items = new List<ManualImportItem>();
            foreach (var resource in resources)
            {
                items.Add(new ManualImportItem
                {
                    Id = resource.Id,
                    Path = resource.Path,
                    Name = resource.Name,
                    Size = resource.Size,
                    Author = resource.Author == null ? null : _authorService.GetAuthor(resource.Author.Id),
                    Book = resource.Book == null ? null : _bookService.GetBook(resource.Book.Id),
                    Edition = resource.ForeignEditionId == null ? null : _editionService.GetEditionByForeignEditionId(resource.ForeignEditionId),
                    Quality = resource.Quality,
                    DownloadId = resource.DownloadId,
                    AdditionalFile = resource.AdditionalFile,
                    ReplaceExistingFiles = resource.ReplaceExistingFiles,
                    DisableReleaseSwitching = resource.DisableReleaseSwitching
                });
            }

            return _manualImportService.UpdateItems(items).Select(x => x.ToResource()).ToList();
        }
    }
}
