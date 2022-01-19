using NzbDrone.Common.Exceptions;

namespace NzbDrone.Core.Exceptions
{
    public class EditionNotFoundException : NzbDroneException
    {
        public string MusicBrainzId { get; set; }

        public EditionNotFoundException(string musicbrainzId)
            : base(string.Format("Edition with id {0} was not found, it may have been removed from metadata server.", musicbrainzId))
        {
            MusicBrainzId = musicbrainzId;
        }

        public EditionNotFoundException(string musicbrainzId, string message, params object[] args)
            : base(message, args)
        {
            MusicBrainzId = musicbrainzId;
        }

        public EditionNotFoundException(string musicbrainzId, string message)
            : base(message)
        {
            MusicBrainzId = musicbrainzId;
        }
    }
}
