using System;
using System.Collections.Generic;

namespace NzbDrone.Core.MetadataSource.BookInfo
{
    public class RecentUpdatesResource
    {
        public bool Limited { get; set; }
        public DateTime Since { get; set; }
        public List<int> Ids { get; set; }
    }
}
