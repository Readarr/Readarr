using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;
using NzbDrone.Core.MetadataSource;
using NzbDrone.Core.MetadataSource.Goodreads;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.BookImport.Identification
{
    public interface ICandidateService
    {
        List<CandidateEdition> GetDbCandidatesFromTags(LocalEdition localEdition, IdentificationOverrides idOverrides, bool includeExisting);
        IEnumerable<CandidateEdition> GetRemoteCandidates(LocalEdition localEdition, IdentificationOverrides idOverrides);
    }

    public class CandidateService : ICandidateService
    {
        private readonly ISearchForNewBook _bookSearchService;
        private readonly IAuthorService _authorService;
        private readonly IBookService _bookService;
        private readonly IEditionService _editionService;
        private readonly IMediaFileService _mediaFileService;
        private readonly Logger _logger;

        public CandidateService(ISearchForNewBook bookSearchService,
                                IAuthorService authorService,
                                IBookService bookService,
                                IEditionService editionService,
                                IMediaFileService mediaFileService,
                                Logger logger)
        {
            _bookSearchService = bookSearchService;
            _authorService = authorService;
            _bookService = bookService;
            _editionService = editionService;
            _mediaFileService = mediaFileService;
            _logger = logger;
        }

        public List<CandidateEdition> GetDbCandidatesFromTags(LocalEdition localEdition, IdentificationOverrides idOverrides, bool includeExisting)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            // Generally author, book and release are null.  But if they're not then limit candidates appropriately.
            // We've tried to make sure that tracks are all for a single release.
            List<CandidateEdition> candidateReleases;

            // if we have a Book ID, use that
            Book tagMbidRelease = null;
            List<CandidateEdition> tagCandidate = null;

            // TODO: select by ISBN?
            // var releaseIds = localEdition.LocalTracks.Select(x => x.FileTrackInfo.ReleaseMBId).Distinct().ToList();
            // if (releaseIds.Count == 1 && releaseIds[0].IsNotNullOrWhiteSpace())
            // {
            //     _logger.Debug("Selecting release from consensus ForeignReleaseId [{0}]", releaseIds[0]);
            //     tagMbidRelease = _releaseService.GetReleaseByForeignReleaseId(releaseIds[0], true);

            //     if (tagMbidRelease != null)
            //     {
            //         tagCandidate = GetDbCandidatesByRelease(new List<BookRelease> { tagMbidRelease }, includeExisting);
            //     }
            // }
            if (idOverrides?.Edition != null)
            {
                var release = idOverrides.Edition;
                _logger.Debug("Edition {0} was forced", release);
                candidateReleases = GetDbCandidatesByEdition(new List<Edition> { release }, includeExisting);
            }
            else if (idOverrides?.Book != null)
            {
                // use the release from file tags if it exists and agrees with the specified book
                if (tagMbidRelease?.Id == idOverrides.Book.Id)
                {
                    candidateReleases = tagCandidate;
                }
                else
                {
                    candidateReleases = GetDbCandidatesByBook(idOverrides.Book, includeExisting);
                }
            }
            else if (idOverrides?.Author != null)
            {
                // use the release from file tags if it exists and agrees with the specified book
                if (tagMbidRelease?.AuthorMetadataId == idOverrides.Author.AuthorMetadataId)
                {
                    candidateReleases = tagCandidate;
                }
                else
                {
                    candidateReleases = GetDbCandidatesByAuthor(localEdition, idOverrides.Author, includeExisting);
                }
            }
            else
            {
                if (tagMbidRelease != null)
                {
                    candidateReleases = tagCandidate;
                }
                else
                {
                    candidateReleases = GetDbCandidates(localEdition, includeExisting);
                }
            }

            watch.Stop();
            _logger.Debug($"Getting {candidateReleases.Count} candidates from tags for {localEdition.LocalBooks.Count} tracks took {watch.ElapsedMilliseconds}ms");

            return candidateReleases;
        }

        private List<CandidateEdition> GetDbCandidatesByEdition(List<Edition> editions, bool includeExisting)
        {
            // get the local tracks on disk for each book
            var bookFiles = editions.Select(x => x.BookId)
                .Distinct()
                .ToDictionary(id => id, id => includeExisting ? _mediaFileService.GetFilesByBook(id) : new List<BookFile>());

            return editions.Select(x => new CandidateEdition
            {
                Edition = x,
                ExistingFiles = bookFiles[x.BookId]
            }).ToList();
        }

        private List<CandidateEdition> GetDbCandidatesByBook(Book book, bool includeExisting)
        {
            // Sort by most voted so less likely to swap to a random release
            return GetDbCandidatesByEdition(_editionService.GetEditionsByBook(book.Id)
                                            .OrderByDescending(x => x.Ratings.Popularity)
                                            .ToList(), includeExisting);
        }

        private List<CandidateEdition> GetDbCandidatesByAuthor(LocalEdition localEdition, Author author, bool includeExisting)
        {
            _logger.Trace("Getting candidates for {0}", author);
            var candidateReleases = new List<CandidateEdition>();

            var bookTag = localEdition.LocalBooks.MostCommon(x => x.FileTrackInfo.BookTitle) ?? "";
            if (bookTag.IsNotNullOrWhiteSpace())
            {
                var possibleBooks = _bookService.GetCandidates(author.AuthorMetadataId, bookTag);
                foreach (var book in possibleBooks)
                {
                    candidateReleases.AddRange(GetDbCandidatesByBook(book, includeExisting));
                }
            }

            return candidateReleases;
        }

        private List<CandidateEdition> GetDbCandidates(LocalEdition localEdition, bool includeExisting)
        {
            // most general version, nothing has been specified.
            // get all plausible authors, then all plausible books, then get releases for each of these.
            var candidateReleases = new List<CandidateEdition>();

            // check if it looks like VA.
            if (TrackGroupingService.IsVariousAuthors(localEdition.LocalBooks))
            {
                var va = _authorService.FindById(DistanceCalculator.VariousAuthorIds[0]);
                if (va != null)
                {
                    candidateReleases.AddRange(GetDbCandidatesByAuthor(localEdition, va, includeExisting));
                }
            }

            var authorTags = localEdition.LocalBooks.MostCommon(x => x.FileTrackInfo.Authors) ?? new List<string>();
            if (authorTags.Any())
            {
                var variants = DistanceCalculator.GetAuthorVariants(authorTags.Where(x => x.IsNotNullOrWhiteSpace()).ToList());

                foreach (var authorTag in variants)
                {
                    if (authorTag.IsNotNullOrWhiteSpace())
                    {
                        var possibleAuthors = _authorService.GetCandidates(authorTag);
                        foreach (var author in possibleAuthors)
                        {
                            candidateReleases.AddRange(GetDbCandidatesByAuthor(localEdition, author, includeExisting));
                        }
                    }
                }
            }

            return candidateReleases;
        }

        public IEnumerable<CandidateEdition> GetRemoteCandidates(LocalEdition localEdition, IdentificationOverrides idOverrides)
        {
            // TODO handle edition override

            // Gets candidate book releases from the metadata server.
            // Will eventually need adding locally if we find a match
            List<Book> remoteBooks;
            var seenCandidates = new HashSet<string>();

            var isbns = localEdition.LocalBooks.Select(x => x.FileTrackInfo.Isbn).Distinct().ToList();
            var asins = localEdition.LocalBooks.Select(x => x.FileTrackInfo.Asin).Distinct().ToList();
            var goodreads = localEdition.LocalBooks.Select(x => x.FileTrackInfo.GoodreadsId).Distinct().ToList();

            // grab possibilities for all the IDs present
            if (isbns.Count == 1 && isbns[0].IsNotNullOrWhiteSpace())
            {
                _logger.Trace($"Searching by isbn {isbns[0]}");

                try
                {
                    remoteBooks = _bookSearchService.SearchByIsbn(isbns[0]);
                }
                catch (GoodreadsException e)
                {
                    _logger.Info(e, "Skipping ISBN search due to Goodreads Error");
                    remoteBooks = new List<Book>();
                }

                foreach (var candidate in ToCandidates(remoteBooks, seenCandidates, idOverrides))
                {
                    yield return candidate;
                }
            }

            if (asins.Count == 1 &&
                asins[0].IsNotNullOrWhiteSpace() &&
                asins[0].Length == 10)
            {
                _logger.Trace($"Searching by asin {asins[0]}");

                try
                {
                    remoteBooks = _bookSearchService.SearchByAsin(asins[0]);
                }
                catch (GoodreadsException e)
                {
                    _logger.Info(e, "Skipping ASIN search due to Goodreads Error");
                    remoteBooks = new List<Book>();
                }

                foreach (var candidate in ToCandidates(remoteBooks, seenCandidates, idOverrides))
                {
                    yield return candidate;
                }
            }

            if (goodreads.Count == 1 &&
                goodreads[0].IsNotNullOrWhiteSpace())
            {
                if (int.TryParse(goodreads[0], out var id))
                {
                    _logger.Trace($"Searching by goodreads id {id}");

                    try
                    {
                        remoteBooks = _bookSearchService.SearchByGoodreadsBookId(id);
                    }
                    catch (GoodreadsException e)
                    {
                        _logger.Info(e, "Skipping Goodreads ID search due to Goodreads Error");
                        remoteBooks = new List<Book>();
                    }

                    foreach (var candidate in ToCandidates(remoteBooks, seenCandidates, idOverrides))
                    {
                        yield return candidate;
                    }
                }
            }

            // If we got an id result, or any overrides are set, stop
            if (seenCandidates.Any() ||
                idOverrides?.Edition != null ||
                idOverrides?.Book != null ||
                idOverrides?.Author != null)
            {
                yield break;
            }

            // fall back to author / book name search
            var authorTags = new List<string>();

            if (TrackGroupingService.IsVariousAuthors(localEdition.LocalBooks))
            {
                authorTags.Add("Various Authors");
            }
            else
            {
                // the most common list of authors reported by a file
                var authors = localEdition.LocalBooks.Select(x => x.FileTrackInfo.Authors.Where(a => a.IsNotNullOrWhiteSpace()).ToList())
                    .GroupBy(x => x.ConcatToString())
                    .OrderByDescending(x => x.Count())
                    .First()
                    .First();
                authorTags.AddRange(authors);
            }

            var bookTag = localEdition.LocalBooks.MostCommon(x => x.FileTrackInfo.BookTitle) ?? "";

            // If no valid author or book tags, stop
            if (!authorTags.Any() || bookTag.IsNullOrWhiteSpace())
            {
                yield break;
            }

            // Search by author+book
            foreach (var authorTag in authorTags)
            {
                try
                {
                    remoteBooks = _bookSearchService.SearchForNewBook(bookTag, authorTag);
                }
                catch (GoodreadsException e)
                {
                    _logger.Info(e, "Skipping author/title search due to Goodreads Error");
                    remoteBooks = new List<Book>();
                }

                foreach (var candidate in ToCandidates(remoteBooks, seenCandidates, idOverrides))
                {
                    yield return candidate;
                }
            }

            // If we got an author/book search result, stop
            if (seenCandidates.Any())
            {
                yield break;
            }

            // Search by just book title
            try
            {
                remoteBooks = _bookSearchService.SearchForNewBook(bookTag, null);
            }
            catch (GoodreadsException e)
            {
                _logger.Info(e, "Skipping book title search due to Goodreads Error");
                remoteBooks = new List<Book>();
            }

            foreach (var candidate in ToCandidates(remoteBooks, seenCandidates, idOverrides))
            {
                yield return candidate;
            }

            // Search by just author
            foreach (var a in authorTags)
            {
                try
                {
                    remoteBooks = _bookSearchService.SearchForNewBook(a, null);
                }
                catch (GoodreadsException e)
                {
                    _logger.Info(e, "Skipping author search due to Goodreads Error");
                    remoteBooks = new List<Book>();
                }

                foreach (var candidate in ToCandidates(remoteBooks, seenCandidates, idOverrides))
                {
                    yield return candidate;
                }
            }
        }

        private List<CandidateEdition> ToCandidates(IEnumerable<Book> books, HashSet<string> seenCandidates, IdentificationOverrides idOverrides)
        {
            var candidates = new List<CandidateEdition>();

            foreach (var book in books)
            {
                // We have to make sure various bits and pieces are populated that are normally handled
                // by a database lazy load
                foreach (var edition in book.Editions.Value)
                {
                    edition.Book = book;

                    if (!seenCandidates.Contains(edition.ForeignEditionId) && SatisfiesOverride(edition, idOverrides))
                    {
                        seenCandidates.Add(edition.ForeignEditionId);
                        candidates.Add(new CandidateEdition
                        {
                            Edition = edition,
                            ExistingFiles = new List<BookFile>()
                        });
                    }
                }
            }

            return candidates;
        }

        private bool SatisfiesOverride(Edition edition, IdentificationOverrides idOverride)
        {
            if (idOverride?.Edition != null)
            {
                return edition.ForeignEditionId == idOverride.Edition.ForeignEditionId;
            }

            if (idOverride?.Book != null)
            {
                return edition.Book.Value.ForeignBookId == idOverride.Book.ForeignBookId;
            }

            if (idOverride?.Author != null)
            {
                return edition.Book.Value.Author.Value.ForeignAuthorId == idOverride.Author.ForeignAuthorId;
            }

            return true;
        }
    }
}
