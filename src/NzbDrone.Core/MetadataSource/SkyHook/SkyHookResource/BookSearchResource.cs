using System.Collections.Generic;

namespace NzbDrone.Core.MetadataSource.SkyHook
{
    public class BookSearchResource
    {
        public List<AuthorSummaryResource> AuthorMetadata { get; set; } = new List<AuthorSummaryResource>();
        public List<BookResource> Books { get; set; } = new List<BookResource>();
    }
}
