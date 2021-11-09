using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

namespace NzbDrone.Core.MetadataSource.Goodreads
{
    /// <summary>
    /// This class models the best book in a work, as defined by the Goodreads API.
    /// </summary>
    [DebuggerDisplay("{DebuggerDisplay,nq}")]
    public sealed class ShowSeriesResource : GoodreadsResource
    {
        public override string ElementName => "series";

        public SeriesResource Series { get; private set; }

        public override void Parse(XElement element)
        {
            Series = new SeriesResource();
            Series.Parse(element);

            Series.Works = element.ParseChildren<WorkResource>("series_works", "series_work");
        }
    }
}
