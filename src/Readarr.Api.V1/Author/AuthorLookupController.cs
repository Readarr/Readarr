using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource;
using Readarr.Http;

namespace Readarr.Api.V1.Author
{
    [V1ApiController("author/lookup")]
    public class AuthorLookupController : Controller
    {
        private readonly ISearchForNewAuthor _searchProxy;

        public AuthorLookupController(ISearchForNewAuthor searchProxy)
        {
            _searchProxy = searchProxy;
        }

        [HttpGet]
        public object Search([FromQuery] string term)
        {
            var searchResults = _searchProxy.SearchForNewAuthor(term);
            return MapToResource(searchResults).ToList();
        }

        private static IEnumerable<AuthorResource> MapToResource(IEnumerable<NzbDrone.Core.Books.Author> author)
        {
            foreach (var currentAuthor in author)
            {
                var resource = currentAuthor.ToResource();
                var poster = currentAuthor.Metadata.Value.Images.FirstOrDefault(c => c.CoverType == MediaCoverTypes.Poster);
                if (poster != null)
                {
                    resource.RemotePoster = poster.Url;
                }

                yield return resource;
            }
        }
    }
}
