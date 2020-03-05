using System;
using System.Collections.Generic;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.MetadataSource
{
    public interface IProvideAuthorInfo
    {
        Author GetAuthorInfo(string readarrId, int metadataProfileId);
        HashSet<string> GetChangedArtists(DateTime startTime);
    }
}
