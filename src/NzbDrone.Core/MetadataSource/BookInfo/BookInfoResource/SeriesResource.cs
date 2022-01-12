using System.Collections.Generic;

namespace NzbDrone.Core.MetadataSource.BookInfo
{
    public class SeriesResource
    {
        public int ForeignId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        public List<SeriesWorkLinkResource> LinkItems { get; set; }
    }

    public class SeriesWorkLinkResource
    {
        public int ForeignWorkId { get; set; }
        public string PositionInSeries { get; set; }
        public int SeriesPosition { get; set; }
        public bool Primary { get; set; }
    }
}
