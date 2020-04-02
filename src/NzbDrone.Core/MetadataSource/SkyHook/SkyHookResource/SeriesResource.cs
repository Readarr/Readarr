using System.Collections.Generic;

namespace NzbDrone.Core.MetadataSource.SkyHook
{
    public class SeriesResource
    {
        public string ForeignId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        public List<SeriesBookLinkResource> BookLinks { get; set; }
    }

    public class SeriesBookLinkResource
    {
        public string SeriesForeignId { get; set; }
        public string BookForeignId { get; set; }
        public string Position { get; set; }
        public bool Primary { get; set; }
    }
}
