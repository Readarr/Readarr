using System.Xml.Linq;

namespace NzbDrone.Core.MetadataSource.SkyHook
{
    public abstract class GoodreadsResource
    {
        public abstract string ElementName { get; }

        public abstract void Parse(XElement element);
    }
}
