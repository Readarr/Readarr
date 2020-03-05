using NzbDrone.Common.Messaging;
using NzbDrone.Core.Music;

namespace NzbDrone.Core.MediaFiles.Events
{
    public class TrackFileRenamedEvent : IEvent
    {
        public Author Artist { get; private set; }
        public TrackFile TrackFile { get; private set; }
        public string OriginalPath { get; private set; }

        public TrackFileRenamedEvent(Author artist, TrackFile trackFile, string originalPath)
        {
            Artist = artist;
            TrackFile = trackFile;
            OriginalPath = originalPath;
        }
    }
}
