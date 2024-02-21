using Newtonsoft.Json;
using Readarr.Http.REST;

namespace Readarr.Api.V1.Indexers
{
    public class IndexerFlagResource : RestResource
    {
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Include)]
        public new int Id { get; set; }
        public string Name { get; set; }
        public string NameLower => Name.ToLowerInvariant();
    }
}
