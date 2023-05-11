using System.Collections.Generic;

namespace NzbDrone.Core.MetadataSource.BookInfo
{
    public class AuthorResource
    {
        public int ForeignId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string Url { get; set; }
        public int RatingCount { get; set; }
        public double AverageRating { get; set; }
        public List<WorkResource> Works { get; set; }
        public List<SeriesResource> Series { get; set; }
    }
}
