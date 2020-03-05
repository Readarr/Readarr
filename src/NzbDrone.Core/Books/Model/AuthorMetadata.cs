using System.Collections.Generic;
using System.Linq;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Music
{
    public class AuthorMetadata : Entity<AuthorMetadata>
    {
        public AuthorMetadata()
        {
            Images = new List<MediaCover.MediaCover>();
            Genres = new List<string>();
            Members = new List<Member>();
            Links = new List<Links>();
            OldForeignArtistIds = new List<string>();
            Aliases = new List<string>();
            Ratings = new Ratings();
        }

        public string ForeignAuthorId { get; set; }
        public List<string> OldForeignArtistIds { get; set; }
        public string Name { get; set; }
        public List<string> Aliases { get; set; }
        public string Overview { get; set; }
        public string Disambiguation { get; set; }
        public string Type { get; set; }
        public ArtistStatusType Status { get; set; }
        public List<MediaCover.MediaCover> Images { get; set; }
        public List<Links> Links { get; set; }
        public List<string> Genres { get; set; }
        public Ratings Ratings { get; set; }
        public List<Member> Members { get; set; }

        public override string ToString()
        {
            return string.Format("[{0}][{1}]", ForeignAuthorId, Name.NullSafe());
        }

        public override void UseMetadataFrom(AuthorMetadata other)
        {
            ForeignAuthorId = other.ForeignAuthorId;
            OldForeignArtistIds = other.OldForeignArtistIds;
            Name = other.Name;
            Aliases = other.Aliases;
            Overview = other.Overview.IsNullOrWhiteSpace() ? Overview : other.Overview;
            Disambiguation = other.Disambiguation;
            Type = other.Type;
            Status = other.Status;
            Images = other.Images.Any() ? other.Images : Images;
            Links = other.Links;
            Genres = other.Genres;
            Ratings = other.Ratings.Votes > 0 ? other.Ratings : Ratings;
            Members = other.Members;
        }
    }
}
