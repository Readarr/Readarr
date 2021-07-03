using System.Collections.Generic;

namespace NzbDrone.Core.ImportLists.Readarr
{
    public class ReadarrAuthor
    {
        public string AuthorName { get; set; }
        public string ForeignAuthorId { get; set; }
        public string Overview { get; set; }
        public List<MediaCover.MediaCover> Images { get; set; }
        public bool Monitored { get; set; }
        public int QualityProfileId { get; set; }
        public HashSet<int> Tags { get; set; }
    }

    public class ReadarrProfile
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }

    public class ReadarrTag
    {
        public string Label { get; set; }
        public int Id { get; set; }
    }
}
