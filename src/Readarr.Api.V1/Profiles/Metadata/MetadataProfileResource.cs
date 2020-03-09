using System.Collections.Generic;
using System.Linq;
using NzbDrone.Core.Profiles.Metadata;
using Readarr.Http.REST;

namespace Readarr.Api.V1.Profiles.Metadata
{
    public class MetadataProfileResource : RestResource
    {
        public string Name { get; set; }
        public double MinRating { get; set; }
        public int MinRatingCount { get; set; }
    }

    public static class MetadataProfileResourceMapper
    {
        public static MetadataProfileResource ToResource(this MetadataProfile model)
        {
            if (model == null)
            {
                return null;
            }

            return new MetadataProfileResource
            {
                Id = model.Id,
                Name = model.Name,
                MinRating = model.MinRating,
                MinRatingCount = model.MinRatingCount
            };
        }

        public static MetadataProfile ToModel(this MetadataProfileResource resource)
        {
            if (resource == null)
            {
                return null;
            }

            return new MetadataProfile
            {
                Id = resource.Id,
                Name = resource.Name,
                MinRating = resource.MinRating,
                MinRatingCount = resource.MinRatingCount
            };
        }

        public static List<MetadataProfileResource> ToResource(this IEnumerable<MetadataProfile> models)
        {
            return models.Select(ToResource).ToList();
        }
    }
}
