using System.Collections.Generic;
using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Music.Events
{
    public class ArtistsImportedEvent : IEvent
    {
        public List<int> AuthorIds { get; private set; }

        public ArtistsImportedEvent(List<int> authorIds)
        {
            AuthorIds = authorIds;
        }
    }
}
