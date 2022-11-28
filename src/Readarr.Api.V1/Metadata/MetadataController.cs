using NzbDrone.Core.Extras.Metadata;
using Readarr.Http;

namespace Readarr.Api.V1.Metadata
{
    [V1ApiController]
    public class MetadataController : ProviderControllerBase<MetadataResource, IMetadata, MetadataDefinition>
    {
        public static readonly MetadataResourceMapper ResourceMapper = new MetadataResourceMapper();

        public MetadataController(IMetadataFactory metadataFactory)
            : base(metadataFactory, "metadata", ResourceMapper)
        {
        }
    }
}
