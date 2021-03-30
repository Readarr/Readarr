using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.MediaFiles;
using Readarr.Http;
using Readarr.Http.REST;

namespace Readarr.Api.V1.Books
{
    [V1ApiController("retag")]
    public class RetagBookController : Controller
    {
        private readonly IEBookTagService _eBookTagService;

        public RetagBookController(IEBookTagService eBookTagService)
        {
            _eBookTagService = eBookTagService;
        }

        [HttpGet]
        public List<RetagBookResource> GetBooks(int? authorId, int? bookId)
        {
            if (bookId.HasValue)
            {
                return _eBookTagService.GetRetagPreviewsByBook(bookId.Value).Where(x => x.Changes.Any()).ToResource();
            }
            else if (authorId.HasValue)
            {
                return _eBookTagService.GetRetagPreviewsByAuthor(authorId.Value).Where(x => x.Changes.Any()).ToResource();
            }
            else
            {
                throw new BadRequestException("One of authorId or bookId must be specified");
            }
        }
    }
}
