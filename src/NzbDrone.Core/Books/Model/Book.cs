using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json.Serialization;
using Equ;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Datastore;
using NzbDrone.Core.MediaFiles;

namespace NzbDrone.Core.Books
{
    [DebuggerDisplay("{GetType().FullName} ID = {Id} [{ForeignBookId}][{Title}]")]
    public class Book : Entity<Book>
    {
        public Book()
        {
            Links = new List<Links>();
            Genres = new List<string>();
            RelatedBooks = new List<int>();
            Ratings = new Ratings();
            Author = new Author();
            AddOptions = new AddBookOptions();
        }

        // These correspond to columns in the Books table
        // These are metadata entries
        public int AuthorMetadataId { get; set; }
        public string ForeignBookId { get; set; }
        public string ForeignEditionId { get; set; }
        public string TitleSlug { get; set; }
        public string Title { get; set; }
        public DateTime? ReleaseDate { get; set; }
        public List<Links> Links { get; set; }
        public List<string> Genres { get; set; }
        public List<int> RelatedBooks { get; set; }
        public Ratings Ratings { get; set; }
        public DateTime? LastSearchTime { get; set; }

        // These are Readarr generated/config
        public string CleanTitle { get; set; }
        public bool Monitored { get; set; }
        public bool AnyEditionOk { get; set; }
        public DateTime? LastInfoSync { get; set; }
        public DateTime Added { get; set; }
        [MemberwiseEqualityIgnore]
        public AddBookOptions AddOptions { get; set; }

        // These are dynamically queried from other tables
        [MemberwiseEqualityIgnore]
        public LazyLoaded<AuthorMetadata> AuthorMetadata { get; set; }
        [MemberwiseEqualityIgnore]
        public LazyLoaded<Author> Author { get; set; }
        [MemberwiseEqualityIgnore]
        public LazyLoaded<List<Edition>> Editions { get; set; }
        [MemberwiseEqualityIgnore]
        public LazyLoaded<List<BookFile>> BookFiles { get; set; }
        [MemberwiseEqualityIgnore]
        public LazyLoaded<List<SeriesBookLink>> SeriesLinks { get; set; }

        //compatibility properties with old version of Book
        [MemberwiseEqualityIgnore]
        [JsonIgnore]
        public int AuthorId
        {
            get { return Author?.Value?.Id ?? 0; } set { Author.Value.Id = value; }
        }

        public override string ToString()
        {
            return string.Format("[{0}][{1}]", ForeignBookId, Title.NullSafe());
        }

        public override void UseMetadataFrom(Book other)
        {
            ForeignBookId = other.ForeignBookId;
            ForeignEditionId = other.ForeignEditionId;
            TitleSlug = other.TitleSlug;
            Title = other.Title;
            ReleaseDate = other.ReleaseDate;
            Links = other.Links;
            Genres = other.Genres;
            RelatedBooks = other.RelatedBooks;
            Ratings = other.Ratings;
            CleanTitle = other.CleanTitle;
        }

        public override void UseDbFieldsFrom(Book other)
        {
            Id = other.Id;
            AuthorMetadataId = other.AuthorMetadataId;
            Monitored = other.Monitored;
            AnyEditionOk = other.AnyEditionOk;
            LastInfoSync = other.LastInfoSync;
            LastSearchTime = other.LastSearchTime;
            Added = other.Added;
            AddOptions = other.AddOptions;
        }

        public override void ApplyChanges(Book other)
        {
            ForeignBookId = other.ForeignBookId;
            ForeignEditionId = other.ForeignEditionId;
            AddOptions = other.AddOptions;
            Monitored = other.Monitored;
            AnyEditionOk = other.AnyEditionOk;
        }
    }
}
