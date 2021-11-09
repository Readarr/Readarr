using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using NzbDrone.Core.Books;

namespace NzbDrone.Core.MetadataSource.Goodreads
{
    /// <summary>
    /// Represents information about a book series as defined by the Goodreads API.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public sealed class ListResource : GoodreadsResource
    {
        public ListResource()
        {
        }

        public override string ElementName => "list";

        public int Page { get; private set; }

        public int PerPage { get; private set; }

        public int ListBooksCount { get; private set; }

        public List<BookResource> Books { get; set; }

        public override void Parse(XElement element)
        {
            Page = element.ElementAsInt("page");
            PerPage = element.ElementAsInt("per_page");
            ListBooksCount = element.ElementAsInt("total_books");

            Books = element.ParseChildren<BookResource>("books", "book") ?? new List<BookResource>();
        }
    }
}
