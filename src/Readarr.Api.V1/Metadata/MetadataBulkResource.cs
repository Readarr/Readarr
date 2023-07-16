using NzbDrone.Core.Extras.Metadata;

namespace Readarr.Api.V1.Metadata
{
    public class MetadataBulkResource : ProviderBulkResource<MetadataBulkResource>
    {
    }

    public class MetadataBulkResourceMapper : ProviderBulkResourceMapper<MetadataBulkResource, MetadataDefinition>
    {
    }
}
