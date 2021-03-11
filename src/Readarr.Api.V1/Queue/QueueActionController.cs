using Microsoft.AspNetCore.Mvc;
using NzbDrone.Core.Download;
using NzbDrone.Core.Download.Pending;
using Readarr.Http;
using Readarr.Http.REST;

namespace Readarr.Api.V1.Queue
{
    [V1ApiController("queue")]
    public class QueueActionController : Controller
    {
        private readonly IPendingReleaseService _pendingReleaseService;
        private readonly IDownloadService _downloadService;

        public QueueActionController(IPendingReleaseService pendingReleaseService,
                                     IDownloadService downloadService)
        {
            _pendingReleaseService = pendingReleaseService;
            _downloadService = downloadService;
        }

        [HttpPost("grab/{id:int}")]
        public object Grab(int id)
        {
            var pendingRelease = _pendingReleaseService.FindPendingQueueItem(id);

            if (pendingRelease == null)
            {
                throw new NotFoundException();
            }

            _downloadService.DownloadReport(pendingRelease.RemoteBook);

            return new object();
        }

        [HttpPost("grab/bulk")]
        public object Grab([FromBody] QueueBulkResource resource)
        {
            foreach (var id in resource.Ids)
            {
                var pendingRelease = _pendingReleaseService.FindPendingQueueItem(id);

                if (pendingRelease == null)
                {
                    throw new NotFoundException();
                }

                _downloadService.DownloadReport(pendingRelease.RemoteBook);
            }

            return new object();
        }
    }
}
