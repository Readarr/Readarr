using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Profiles.Metadata
{
    public class MetadataProfile : ModelBase
    {
        public string Name { get; set; }
        public double MinRating { get; set; }
        public int MinRatingCount { get; set; }
    }
}
