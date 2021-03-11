using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.MediaFiles;
using Readarr.Http;

namespace Readarr.Api.V1.Books
{
    [V1ApiController("rename")]
    public class RenameBookController : Controller
    {
        private readonly IRenameBookFileService _renameBookFileService;

        public RenameBookController(IRenameBookFileService renameBookFileService)
        {
            _renameBookFileService = renameBookFileService;
        }

        [HttpGet]
        public List<RenameBookResource> GetBookFiles(int authorId, int? bookId)
        {
            if (bookId.HasValue)
            {
                return _renameBookFileService.GetRenamePreviews(authorId, bookId.Value).ToResource();
            }

            return _renameBookFileService.GetRenamePreviews(authorId).ToResource();
        }
    }
}
