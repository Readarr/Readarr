using System.Collections.Generic;
using Readarr.Api.V1.Books;
using Readarr.Http;
using RestSharp;

namespace NzbDrone.Integration.Test.Client
{
    public class WantedClient : ClientBase<BookResource>
    {
        public WantedClient(IRestClient restClient, string apiKey, string resource)
            : base(restClient, apiKey, resource)
        {
        }

        public PagingResource<BookResource> GetPagedIncludeAuthor(int pageNumber, int pageSize, string sortKey, string sortDir, string filterKey = null, string filterValue = null, bool includeAuthor = true)
        {
            var request = BuildRequest();
            request.AddParameter("page", pageNumber);
            request.AddParameter("pageSize", pageSize);
            request.AddParameter("sortKey", sortKey);
            request.AddParameter("sortDir", sortDir);

            if (filterKey != null && filterValue != null)
            {
                request.AddParameter("filterKey", filterKey);
                request.AddParameter("filterValue", filterValue);
            }

            request.AddParameter("includeAuthor", includeAuthor);

            return Get<PagingResource<BookResource>>(request);
        }

        public List<BookResource> GetBooksInAuthor(int authorId)
        {
            var request = BuildRequest("?authorId=" + authorId.ToString());
            return Get<List<BookResource>>(request);
        }
    }
}
