using NzbDrone.Core.MetadataSource.Goodreads;

namespace NzbDrone.Core.MetadataSource
{
    public interface IProvideListInfo
    {
        ListResource GetListInfo(int id, int page, bool useCache = true);
    }
}
