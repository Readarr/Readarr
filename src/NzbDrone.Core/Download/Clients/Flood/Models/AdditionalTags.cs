using NzbDrone.Core.Annotations;

namespace NzbDrone.Core.Download.Clients.Flood.Models
{
    public enum AdditionalTags
    {
        [FieldOption(Hint = "J.R.R. Tolkien")]
        Author = 0,

        [FieldOption(Hint = "EPUB")]
        Format = 1,

        [FieldOption(Hint = "Example-Raws")]
        ReleaseGroup = 2,

        [FieldOption(Hint = "1954")]
        Year = 3,

        [FieldOption(Hint = "Torznab")]
        Indexer = 4,
    }
}
