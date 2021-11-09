using System;
using System.Collections.Generic;

namespace NzbDrone.Core.MetadataSource.BookInfo
{
    public class AuthorResource
    {
        public int ForeignId { get; set; }
        public string Name { get; set; }
        public string TitleSlug { get; set; }

        public string Description { get; set; }
        public string ImageUrl { get; set; }
        public string Url { get; set; }

        public int ReviewCount { get; set; }
        public int RatingCount { get; set; }
        public double AverageRating { get; set; }

        public DateTime LastChange { get; set; }

        public DateTime LastRefresh { get; set; }

        public List<WorkResource> Works { get; set; }

        public List<SeriesResource> Series { get; set; }
    }
}
