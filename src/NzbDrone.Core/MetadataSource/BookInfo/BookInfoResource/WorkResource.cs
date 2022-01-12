using System;
using System.Collections.Generic;

namespace NzbDrone.Core.MetadataSource.BookInfo
{
    public class WorkResource
    {
        public int ForeignId { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public List<string> Genres { get; set; }
        public List<int> RelatedWorks { get; set; }
        public List<BookResource> Books { get; set; }
        public List<SeriesResource> Series { get; set; } = new List<SeriesResource>();
        public List<AuthorResource> Authors { get; set; } = new List<AuthorResource>();
    }
}
