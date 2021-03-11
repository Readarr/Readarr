using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Blacklisting;
using NzbDrone.Core.Datastore;
using NzbDrone.Http.REST.Attributes;
using Readarr.Http;
using Readarr.Http.Extensions;

namespace Readarr.Api.V1.Blacklist
{
    [V1ApiController]
    public class BlacklistController : Controller
    {
        private readonly IBlacklistService _blacklistService;

        public BlacklistController(IBlacklistService blacklistService)
        {
            _blacklistService = blacklistService;
        }

        [HttpGet]
        public PagingResource<BlacklistResource> GetBlacklist()
        {
            var pagingResource = Request.ReadPagingResourceFromRequest<BlacklistResource>();
            var pagingSpec = pagingResource.MapToPagingSpec<BlacklistResource, NzbDrone.Core.Blacklisting.Blacklist>("date", SortDirection.Descending);

            return pagingSpec.ApplyToPage(_blacklistService.Paged, BlacklistResourceMapper.MapToResource);
        }

        [RestDeleteById]
        public void DeleteBlacklist(int id)
        {
            _blacklistService.Delete(id);
        }

        [HttpDelete("bulk")]
        public object Remove([FromBody] BlacklistBulkResource resource)
        {
            _blacklistService.Delete(resource.Ids);

            return new object();
        }
    }
}
