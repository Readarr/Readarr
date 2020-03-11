using Equ;
using NzbDrone.Core.Datastore;

namespace NzbDrone.Core.Music
{
    public class SeriesBookLink : Entity<SeriesBookLink>
    {
        public string ForeignId { get; set; }
        public string Position { get; set; }
        public int SeriesId { get; set; }
        public int BookId { get; set; }

        [MemberwiseEqualityIgnore]
        public LazyLoaded<Series> Series { get; set; }
        [MemberwiseEqualityIgnore]
        public LazyLoaded<Book> Book { get; set; }

        public override void UseMetadataFrom(SeriesBookLink other)
        {
            ForeignId = other.ForeignId;
            Position = other.Position;
        }

        public override void UseDbFieldsFrom(SeriesBookLink other)
        {
            Id = other.Id;
            SeriesId = other.SeriesId;
            BookId = other.BookId;
        }
    }
}
