using System.Collections.Generic;

namespace NzbDrone.Core.MetadataSource.BookInfo
{
    public class BulkBookResource
    {
        public List<WorkResource> Works { get; set; }
        public List<SeriesResource> Series { get; set; }
        public List<AuthorResource> Authors { get; set; }
    }
}
