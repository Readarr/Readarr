using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Blocklisting;
using NzbDrone.Core.Books;
using NzbDrone.Core.History;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.CustomFormats
{
    public interface ICustomFormatCalculationService
    {
        List<CustomFormat> ParseCustomFormat(RemoteBook remoteBook, long size);
        List<CustomFormat> ParseCustomFormat(BookFile bookFile, Author artist);
        List<CustomFormat> ParseCustomFormat(BookFile bookFile);
        List<CustomFormat> ParseCustomFormat(Blocklist blocklist, Author artist);
        List<CustomFormat> ParseCustomFormat(EntityHistory history, Author artist);
        List<CustomFormat> ParseCustomFormat(LocalBook localBook);
    }

    public class CustomFormatCalculationService : ICustomFormatCalculationService
    {
        private readonly ICustomFormatService _formatService;
        private readonly Logger _logger;

        public CustomFormatCalculationService(ICustomFormatService formatService, Logger logger)
        {
            _formatService = formatService;
            _logger = logger;
        }

        public List<CustomFormat> ParseCustomFormat(RemoteBook remoteBook, long size)
        {
            var input = new CustomFormatInput
            {
                BookInfo = remoteBook.ParsedBookInfo,
                Author = remoteBook.Author,
                Size = size,
                IndexerFlags = remoteBook.Release?.IndexerFlags ?? 0
            };

            return ParseCustomFormat(input);
        }

        public List<CustomFormat> ParseCustomFormat(BookFile bookFile, Author author)
        {
            return ParseCustomFormat(bookFile, author, _formatService.All());
        }

        public List<CustomFormat> ParseCustomFormat(BookFile bookFile)
        {
            return ParseCustomFormat(bookFile, bookFile.Author.Value, _formatService.All());
        }

        public List<CustomFormat> ParseCustomFormat(Blocklist blocklist, Author author)
        {
            var parsed = Parser.Parser.ParseBookTitle(blocklist.SourceTitle);

            var bookInfo = new ParsedBookInfo
            {
                AuthorName = author.Name,
                ReleaseTitle = parsed?.ReleaseTitle ?? blocklist.SourceTitle,
                Quality = blocklist.Quality,
                ReleaseGroup = parsed?.ReleaseGroup
            };

            var input = new CustomFormatInput
            {
                BookInfo = bookInfo,
                Author = author,
                Size = blocklist.Size ?? 0,
                IndexerFlags = blocklist.IndexerFlags
            };

            return ParseCustomFormat(input);
        }

        public List<CustomFormat> ParseCustomFormat(EntityHistory history, Author author)
        {
            var parsed = Parser.Parser.ParseBookTitle(history.SourceTitle);

            long.TryParse(history.Data.GetValueOrDefault("size"), out var size);
            Enum.TryParse(history.Data.GetValueOrDefault("indexerFlags"), true, out IndexerFlags indexerFlags);

            var bookInfo = new ParsedBookInfo
            {
                AuthorName = author.Name,
                ReleaseTitle = parsed?.ReleaseTitle ?? history.SourceTitle,
                Quality = history.Quality,
                ReleaseGroup = parsed?.ReleaseGroup,
            };

            var input = new CustomFormatInput
            {
                BookInfo = bookInfo,
                Author = author,
                Size = size,
                IndexerFlags = indexerFlags
            };

            return ParseCustomFormat(input);
        }

        public List<CustomFormat> ParseCustomFormat(LocalBook localBook)
        {
            var bookInfo = new ParsedBookInfo
            {
                AuthorName = localBook.Author.Name,
                ReleaseTitle = localBook.SceneName,
                Quality = localBook.Quality,
                ReleaseGroup = localBook.ReleaseGroup
            };

            var input = new CustomFormatInput
            {
                BookInfo = bookInfo,
                Author = localBook.Author,
                Size = localBook.Size,
                IndexerFlags = localBook.IndexerFlags,
            };

            return ParseCustomFormat(input);
        }

        private List<CustomFormat> ParseCustomFormat(CustomFormatInput input)
        {
            return ParseCustomFormat(input, _formatService.All());
        }

        private static List<CustomFormat> ParseCustomFormat(CustomFormatInput input, List<CustomFormat> allCustomFormats)
        {
            var matches = new List<CustomFormat>();

            foreach (var customFormat in allCustomFormats)
            {
                var specificationMatches = customFormat.Specifications
                    .GroupBy(t => t.GetType())
                    .Select(g => new SpecificationMatchesGroup
                    {
                        Matches = g.ToDictionary(t => t, t => t.IsSatisfiedBy(input))
                    })
                    .ToList();

                if (specificationMatches.All(x => x.DidMatch))
                {
                    matches.Add(customFormat);
                }
            }

            return matches.OrderBy(x => x.Name).ToList();
        }

        private List<CustomFormat> ParseCustomFormat(BookFile bookFile, Author author, List<CustomFormat> allCustomFormats)
        {
            var releaseTitle = string.Empty;

            if (bookFile.SceneName.IsNotNullOrWhiteSpace())
            {
                _logger.Trace("Using scene name for release title: {0}", bookFile.SceneName);
                releaseTitle = bookFile.SceneName;
            }
            else if (bookFile.OriginalFilePath.IsNotNullOrWhiteSpace())
            {
                _logger.Trace("Using original file path for release title: {0}", bookFile.OriginalFilePath);
                releaseTitle = bookFile.OriginalFilePath;
            }
            else if (bookFile.Path.IsNotNullOrWhiteSpace())
            {
                _logger.Trace("Using path for release title: {0}", Path.GetFileName(bookFile.Path));
                releaseTitle = Path.GetFileName(bookFile.Path);
            }

            var bookInfo = new ParsedBookInfo
            {
                AuthorName = author.Name,
                ReleaseTitle = releaseTitle,
                Quality = bookFile.Quality,
                ReleaseGroup = bookFile.ReleaseGroup
            };

            var input = new CustomFormatInput
            {
                BookInfo = bookInfo,
                Author = author,
                Size = bookFile.Size,
                IndexerFlags = bookFile.IndexerFlags,
                Filename = Path.GetFileName(bookFile.Path)
            };

            return ParseCustomFormat(input, allCustomFormats);
        }
    }
}
