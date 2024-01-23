using System.Collections.Generic;

namespace NzbDrone.Core.ImportLists.Readarr
{
    public class ReadarrAuthor
    {
        public string AuthorName { get; set; }
        public int Id { get; set; }
        public string ForeignAuthorId { get; set; }
        public string Overview { get; set; }
        public List<MediaCover.MediaCover> Images { get; set; }
        public bool Monitored { get; set; }
        public int QualityProfileId { get; set; }
        public string RootFolderPath { get; set; }
        public HashSet<int> Tags { get; set; }
    }

    public class ReadarrEdition
    {
        public string Title { get; set; }
        public string ForeignEditionId { get; set; }
        public string Overview { get; set; }
        public List<MediaCover.MediaCover> Images { get; set; }
        public bool Monitored { get; set; }
    }

    public class ReadarrBook
    {
        public string Title { get; set; }
        public string ForeignBookId { get; set; }
        public string ForeignEditionId { get; set; }
        public string Overview { get; set; }
        public List<MediaCover.MediaCover> Images { get; set; }
        public bool Monitored { get; set; }
        public ReadarrAuthor Author { get; set; }
        public int AuthorId { get; set; }
        public List<ReadarrEdition> Editions { get; set; }
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

    public class ReadarrRootFolder
    {
        public string Path { get; set; }
        public int Id { get; set; }
    }
}
