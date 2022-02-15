using System.Collections.Generic;
using System.IO;
using System.Linq;
using NzbDrone.Core.Books;
using NzbDrone.Core.MediaFiles.BookImport.Identification;

namespace NzbDrone.Core.Parser.Model
{
    public class LocalEdition
    {
        public LocalEdition()
        {
            LocalBooks = new List<LocalBook>();

            // A dummy distance, will be replaced
            Distance = new Distance();
            Distance.Add("book_id", 1.0);
        }

        public LocalEdition(List<LocalBook> tracks)
        {
            LocalBooks = tracks;

            // A dummy distance, will be replaced
            Distance = new Distance();
            Distance.Add("book_id", 1.0);
        }

        public List<LocalBook> LocalBooks { get; set; }
        public int TrackCount => LocalBooks.Count;

        public Distance Distance { get; set; }
        public Edition Edition { get; set; }
        public List<LocalBook> ExistingTracks { get; set; }
        public bool NewDownload { get; set; }

        public void PopulateMatch(bool keepAllEditions)
        {
            if (Edition != null)
            {
                LocalBooks = LocalBooks.Concat(ExistingTracks).DistinctBy(x => x.Path).ToList();

                if (!keepAllEditions)
                {
                    // Manually clone the edition / book to avoid holding references to *every* edition we have
                    // seen during the matching process
                    var edition = new Edition();
                    edition.UseMetadataFrom(Edition);
                    edition.UseDbFieldsFrom(Edition);
                    edition.BookFiles = Edition.BookFiles;

                    var fullBook = Edition.Book.Value;

                    var book = new Book();
                    book.UseMetadataFrom(fullBook);
                    book.UseDbFieldsFrom(fullBook);
                    book.Author.Value.UseMetadataFrom(fullBook.Author.Value);
                    book.Author.Value.UseDbFieldsFrom(fullBook.Author.Value);
                    book.Author.Value.Metadata = fullBook.AuthorMetadata.Value;
                    book.AuthorMetadata = fullBook.AuthorMetadata.Value;
                    book.BookFiles = fullBook.BookFiles;
                    book.Editions = new List<Edition> { edition };

                    if (fullBook.SeriesLinks.IsLoaded)
                    {
                        book.SeriesLinks = fullBook.SeriesLinks.Value.Select(l => new SeriesBookLink
                        {
                            Book = book,
                            Series = new Series
                            {
                                ForeignSeriesId = l.Series.Value.ForeignSeriesId,
                                Title = l.Series.Value.Title,
                                Description = l.Series.Value.Description,
                                Numbered = l.Series.Value.Numbered,
                                WorkCount = l.Series.Value.WorkCount,
                                PrimaryWorkCount = l.Series.Value.PrimaryWorkCount
                            },
                            IsPrimary = l.IsPrimary,
                            Position = l.Position,
                            SeriesPosition = l.SeriesPosition
                        }).ToList();
                    }
                    else
                    {
                        book.SeriesLinks = fullBook.SeriesLinks;
                    }

                    edition.Book = book;

                    Edition = edition;

                    foreach (var localTrack in LocalBooks)
                    {
                        localTrack.Edition = edition;
                        localTrack.Book = book;
                        localTrack.Author = book.Author.Value;
                        localTrack.PartCount = LocalBooks.Count;
                    }
                }
                else
                {
                    foreach (var localTrack in LocalBooks)
                    {
                        localTrack.Edition = Edition;
                        localTrack.Book = Edition.Book.Value;
                        localTrack.Author = Edition.Book.Value.Author.Value;
                        localTrack.PartCount = LocalBooks.Count;
                    }
                }
            }
        }

        public override string ToString()
        {
            return "[" + string.Join(", ", LocalBooks.Select(x => Path.GetDirectoryName(x.Path)).Distinct()) + "]";
        }
    }
}
