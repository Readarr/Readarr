using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Runtime.CompilerServices;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Common.Serializer;
using NzbDrone.Core.Books;
using NzbDrone.Core.Books.Calibre;
using NzbDrone.Core.MediaFiles.Azw;
using NzbDrone.Core.MediaFiles.Commands;
using NzbDrone.Core.Messaging.Commands;
using NzbDrone.Core.Parser.Model;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.RootFolders;
using PdfSharpCore.Pdf.IO;
using VersOne.Epub;
using VersOne.Epub.Schema;

namespace NzbDrone.Core.MediaFiles
{
    public interface IEBookTagService
    {
        ParsedTrackInfo ReadTags(IFileInfo file);
        List<RetagBookFilePreview> GetRetagPreviewsByAuthor(int authorId);
        List<RetagBookFilePreview> GetRetagPreviewsByBook(int authorId);
    }

    public class EBookTagService : IEBookTagService,
        IExecute<RetagFilesCommand>,
        IExecute<RetagAuthorCommand>
    {
        private readonly IAuthorService _authorService;
        private readonly IMediaFileService _mediaFileService;
        private readonly IRootFolderService _rootFolderService;
        private readonly ICalibreProxy _calibre;
        private readonly Logger _logger;

        public EBookTagService(IAuthorService authorService,
            IMediaFileService mediaFileService,
            IRootFolderService rootFolderService,
            ICalibreProxy calibre,
            Logger logger)
        {
            _authorService = authorService;
            _mediaFileService = mediaFileService;
            _rootFolderService = rootFolderService;
            _calibre = calibre;

            _logger = logger;
        }

        public ParsedTrackInfo ReadTags(IFileInfo file)
        {
            var extension = file.Extension.ToLower();
            _logger.Trace($"Got extension '{extension}'");

            switch (extension)
            {
                case ".pdf":
                    return ReadPdf(file.FullName);
                case ".epub":
                    return ReadEpub(file.FullName);
                case ".azw3":
                case ".mobi":
                    return ReadAzw3(file.FullName);
                default:
                    return Parser.Parser.ParseTitle(file.FullName);
            }
        }

        public List<RetagBookFilePreview> GetRetagPreviewsByAuthor(int authorId)
        {
            var files = _mediaFileService.GetFilesByAuthor(authorId);

            return GetPreviews(files).ToList();
        }

        public List<RetagBookFilePreview> GetRetagPreviewsByBook(int bookId)
        {
            var files = _mediaFileService.GetFilesByBook(bookId);

            return GetPreviews(files).ToList();
        }

        public void Execute(RetagFilesCommand message)
        {
            var author = _authorService.GetAuthor(message.AuthorId);
            var files = _mediaFileService.Get(message.Files);

            _logger.ProgressInfo("Re-tagging {0} files for {1}", files.Count, author.Name);

            foreach (var file in files.Where(x => x.CalibreId != 0))
            {
                var rootFolder = _rootFolderService.GetBestRootFolder(file.Path);
                _calibre.SetFields(file, rootFolder.CalibreSettings, message.UpdateCovers, message.EmbedMetadata);
            }

            _logger.ProgressInfo("Selected files re-tagged for {0}", author.Name);
        }

        public void Execute(RetagAuthorCommand message)
        {
            _logger.Debug("Re-tagging all files for selected authors");
            var authorsToRename = _authorService.GetAuthors(message.AuthorIds);

            foreach (var author in authorsToRename)
            {
                var files = _mediaFileService.GetFilesByAuthor(author.Id);

                _logger.ProgressInfo("Re-tagging all files for author: {0}", author.Name);

                foreach (var file in files.Where(x => x.CalibreId != 0))
                {
                    var rootFolder = _rootFolderService.GetBestRootFolder(file.Path);
                    _calibre.SetFields(file, rootFolder.CalibreSettings, message.UpdateCovers, message.EmbedMetadata);
                }

                _logger.ProgressInfo("All files re-tagged for {0}", author.Name);
            }
        }

        private IEnumerable<RetagBookFilePreview> GetPreviews(List<BookFile> files)
        {
            var calibreFiles = files.Where(x => x.CalibreId > 0).OrderBy(x => x.Edition.Value.Title).ToList();

            var rootFolderPairs = calibreFiles.Select(x => Tuple.Create(x, _rootFolderService.GetBestRootFolder(x.Path)));

            var rootFolderGroups = rootFolderPairs.GroupBy(x => x.Item2.Path);

            var calibreBooks = new List<CalibreBook>();
            foreach (var group in rootFolderGroups)
            {
                var rootFolder = group.First().Item2;
                var books = _calibre.GetBooks(group.Select(x => x.Item1.CalibreId).ToList(), rootFolder.CalibreSettings);
                calibreBooks.AddRange(books);
            }

            var dict = calibreBooks.ToDictionary(x => x.Id);

            foreach (var file in calibreFiles)
            {
                var edition = file.Edition.Value;
                var book = edition.Book.Value;
                var serieslink = book.SeriesLinks.Value.FirstOrDefault(x => x.Series.Value.Title.IsNotNullOrWhiteSpace());

                var series = serieslink?.Series.Value;
                double? seriesIndex = null;
                if (double.TryParse(serieslink?.Position, out var index))
                {
                    _logger.Trace($"Parsed {serieslink?.Position} as {index}");
                    seriesIndex = index;
                }

                var oldTags = dict[file.CalibreId];

                var newTags = new CalibreBook
                {
                    Title = edition.Title,
                    Authors = new List<string> { file.Author.Value.Name },
                    PubDate = book.ReleaseDate,
                    Publisher = edition.Publisher,
                    Languages = new List<string> { edition.Language.CanonicalizeLanguage() },
                    Comments = edition.Overview,
                    Rating = (int)(edition.Ratings.Value * 2) / 2.0,
                    Identifiers = new Dictionary<string, string>
                    {
                        { "isbn", edition.Isbn13 },
                        { "asin", edition.Asin },
                        { "goodreads", edition.ForeignEditionId }
                    },
                    Series = series?.Title,
                    Position = seriesIndex
                };

                var diff = oldTags.Diff(newTags);

                if (diff.Any())
                {
                    yield return new RetagBookFilePreview
                    {
                        AuthorId = file.Author.Value.Id,
                        BookId = file.Edition.Value.Id,
                        BookFileId = file.Id,
                        Path = file.Path,
                        Changes = diff
                    };
                }
            }
        }

        private ParsedTrackInfo ReadEpub(string file)
        {
            _logger.Trace($"Reading {file}");
            var result = new ParsedTrackInfo
            {
                Quality = new QualityModel
                {
                    Quality = Quality.EPUB,
                    QualityDetectionSource = QualityDetectionSource.TagLib
                }
            };

            try
            {
                using (var bookRef = EpubReader.OpenBook(file))
                {
                    result.AuthorTitle = bookRef.AuthorList.FirstOrDefault();
                    result.BookTitle = bookRef.Title;

                    var meta = bookRef.Schema.Package.Metadata;

                    _logger.Trace(meta.ToJson());

                    result.Isbn = GetIsbn(meta?.Identifiers);
                    result.Asin = meta?.Identifiers?.FirstOrDefault(x => x.Scheme?.ToLower().Contains("asin") ?? false)?.Identifier;
                    result.Language = meta?.Languages?.FirstOrDefault();
                    result.Publisher = meta?.Publishers?.FirstOrDefault();
                    result.Disambiguation = meta?.Description;

                    result.SeriesTitle = meta?.MetaItems?.FirstOrDefault(x => x.Name == "calibre:series")?.Content;
                    result.SeriesIndex = meta?.MetaItems?.FirstOrDefault(x => x.Name == "calibre:series_index")?.Content;
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error reading epub");
                result.Quality.QualityDetectionSource = QualityDetectionSource.Extension;
            }

            _logger.Trace($"Got:\n{result.ToJson()}");

            return result;
        }

        private ParsedTrackInfo ReadAzw3(string file)
        {
            _logger.Trace($"Reading {file}");
            var result = new ParsedTrackInfo();

            try
            {
                var book = new Azw3File(file);
                result.AuthorTitle = book.Author;
                result.BookTitle = book.Title;
                result.Isbn = StripIsbn(book.Isbn);
                result.Asin = book.Asin;
                result.Language = book.Language;
                result.Disambiguation = book.Description;
                result.Publisher = book.Publisher;
                result.Label = book.Imprint;
                result.Source = book.Source;

                result.Quality = new QualityModel
                {
                    Quality = book.Version <= 6 ? Quality.MOBI : Quality.AZW3,
                    QualityDetectionSource = QualityDetectionSource.TagLib
                };
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error reading file");

                result.Quality = new QualityModel
                {
                    Quality = Path.GetExtension(file) == ".mobi" ? Quality.MOBI : Quality.AZW3,
                    QualityDetectionSource = QualityDetectionSource.Extension
                };
            }

            _logger.Trace($"Got {result.ToJson()}");

            return result;
        }

        private ParsedTrackInfo ReadPdf(string file)
        {
            _logger.Trace($"Reading {file}");
            var result = new ParsedTrackInfo
            {
                Quality = new QualityModel
                {
                    Quality = Quality.PDF,
                    QualityDetectionSource = QualityDetectionSource.TagLib
                }
            };

            try
            {
                var book = PdfReader.Open(file, PdfDocumentOpenMode.InformationOnly);
                result.AuthorTitle = book.Info.Author;
                result.BookTitle = book.Info.Title;

                _logger.Trace(book.Info.ToJson());
                _logger.Trace(book.CustomValues.ToJson());
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error reading pdf");
                result.Quality.QualityDetectionSource = QualityDetectionSource.Extension;
            }

            _logger.Trace($"Got:\n{result.ToJson()}");

            return result;
        }

        private string GetIsbn(IEnumerable<EpubMetadataIdentifier> ids)
        {
            foreach (var id in ids)
            {
                var isbn = StripIsbn(id?.Identifier);
                if (isbn != null)
                {
                    return isbn;
                }
            }

            return null;
        }

        private string GetIsbnChars(string input)
        {
            if (input == null)
            {
                return null;
            }

            return new string(input.Where(c => char.IsDigit(c) || c == 'X' || c == 'x').ToArray());
        }

        private string StripIsbn(string input)
        {
            var isbn = GetIsbnChars(input);

            if (isbn == null)
            {
                return null;
            }
            else if ((isbn.Length == 10 && ValidateIsbn10(isbn)) ||
                (isbn.Length == 13 && ValidateIsbn13(isbn)))
            {
                return isbn;
            }

            return null;
        }

        private static char Isbn10Checksum(string isbn)
        {
            var sum = 0;
            for (var i = 0; i < 9; i++)
            {
                sum += int.Parse(isbn[i].ToString()) * (10 - i);
            }

            var result = sum % 11;

            if (result == 0)
            {
                return '0';
            }
            else if (result == 1)
            {
                return 'X';
            }

            return (11 - result).ToString()[0];
        }

        private static char Isbn13Checksum(string isbn)
        {
            var result = 0;
            for (var i = 0; i < 12; i++)
            {
                result += int.Parse(isbn[i].ToString()) * ((i % 2 == 0) ? 1 : 3);
            }

            result %= 10;

            return result == 0 ? '0' : (10 - result).ToString()[0];
        }

        private static bool ValidateIsbn10(string isbn)
        {
            return ulong.TryParse(isbn.Substring(0, 9), out _) && isbn[9] == Isbn10Checksum(isbn);
        }

        private static bool ValidateIsbn13(string isbn)
        {
            return ulong.TryParse(isbn, out _) && isbn[12] == Isbn13Checksum(isbn);
        }
    }
}
