using NzbDrone.Core.Indexers;
using Readarr.Http;

namespace Readarr.Api.V1.Indexers
{
    [V1ApiController]
    public class IndexerController : ProviderControllerBase<IndexerResource, IIndexer, IndexerDefinition>
    {
        public static readonly IndexerResourceMapper ResourceMapper = new IndexerResourceMapper();

        public IndexerController(IndexerFactory indexerFactory)
            : base(indexerFactory, "indexer", ResourceMapper)
        {
        }

        protected override void Validate(IndexerDefinition definition, bool includeWarnings)
        {
            if (!definition.Enable)
            {
                return;
            }

            base.Validate(definition, includeWarnings);
        }
    }
}
