using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.MediaCover;
using NzbDrone.Core.MetadataSource;
using Readarr.Api.V1.Author;
using Readarr.Api.V1.Books;
using Readarr.Http;

namespace Readarr.Api.V1.Search
{
    [V1ApiController]
    public class SearchController : Controller
    {
        private readonly ISearchForNewEntity _searchProxy;

        public SearchController(ISearchForNewEntity searchProxy)
        {
            _searchProxy = searchProxy;
        }

        [HttpGet]
        public object Search([FromQuery] string term)
        {
            var searchResults = _searchProxy.SearchForNewEntity(term);
            return MapToResource(searchResults).ToList();
        }

        private static IEnumerable<SearchResource> MapToResource(IEnumerable<object> results)
        {
            int id = 1;
            foreach (var result in results)
            {
                var resource = new SearchResource();
                resource.Id = id++;

                if (result is NzbDrone.Core.Books.Author)
                {
                    var author = (NzbDrone.Core.Books.Author)result;
                    resource.Author = author.ToResource();
                    resource.ForeignId = author.ForeignAuthorId;

                    var poster = author.Metadata.Value.Images.FirstOrDefault(c => c.CoverType == MediaCoverTypes.Poster);
                    if (poster != null)
                    {
                        resource.Author.RemotePoster = poster.Url;
                    }
                }
                else if (result is NzbDrone.Core.Books.Book)
                {
                    var book = (NzbDrone.Core.Books.Book)result;
                    resource.Book = book.ToResource();
                    resource.Book.Overview = book.Editions.Value.Single(x => x.Monitored).Overview;
                    resource.Book.Author = book.Author.Value.ToResource();
                    resource.Book.Editions = book.Editions.Value.ToResource();
                    resource.ForeignId = book.ForeignBookId;

                    var cover = book.Editions.Value.Single(x => x.Monitored).Images.FirstOrDefault(c => c.CoverType == MediaCoverTypes.Cover);
                    if (cover != null)
                    {
                        resource.Book.RemoteCover = cover.Url;
                    }
                }
                else
                {
                    throw new NotImplementedException("Bad response from search all proxy");
                }

                yield return resource;
            }
        }
    }
}
