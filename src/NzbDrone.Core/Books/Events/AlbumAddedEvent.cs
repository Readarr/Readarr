using NzbDrone.Common.Messaging;

namespace NzbDrone.Core.Music.Events
{
    public class AlbumAddedEvent : IEvent
    {
        public Book Album { get; private set; }

        public AlbumAddedEvent(Book album)
        {
            Album = album;
        }
    }
}
