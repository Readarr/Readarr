using System.Collections.Generic;

namespace Readarr.Api.V1.Books
{
    public class BookEditorResource
    {
        public List<int> BookIds { get; set; }
        public bool? Monitored { get; set; }
        public bool? DeleteFiles { get; set; }
        public bool? AddImportListExclusion { get; set; }
    }
}
