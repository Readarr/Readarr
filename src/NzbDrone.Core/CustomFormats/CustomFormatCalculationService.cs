using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public CustomFormatCalculationService(ICustomFormatService formatService)
        {
            _formatService = formatService;
        }

        public List<CustomFormat> ParseCustomFormat(RemoteBook remoteBook, long size)
        {
            var input = new CustomFormatInput
            {
                BookInfo = remoteBook.ParsedBookInfo,
                Author = remoteBook.Author,
                Size = size
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
                Size = blocklist.Size ?? 0
            };

            return ParseCustomFormat(input);
        }

        public List<CustomFormat> ParseCustomFormat(EntityHistory history, Author author)
        {
            var parsed = Parser.Parser.ParseBookTitle(history.SourceTitle);

            long.TryParse(history.Data.GetValueOrDefault("size"), out var size);

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
                Size = size
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
                Size = localBook.Size
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

            return matches;
        }

        private static List<CustomFormat> ParseCustomFormat(BookFile bookFile, Author author, List<CustomFormat> allCustomFormats)
        {
            var sceneName = string.Empty;
            if (bookFile.SceneName.IsNotNullOrWhiteSpace())
            {
                sceneName = bookFile.SceneName;
            }
            else if (bookFile.OriginalFilePath.IsNotNullOrWhiteSpace())
            {
                sceneName = bookFile.OriginalFilePath;
            }
            else if (bookFile.Path.IsNotNullOrWhiteSpace())
            {
                sceneName = Path.GetFileName(bookFile.Path);
            }

            var bookInfo = new ParsedBookInfo
            {
                AuthorName = author.Name,
                ReleaseTitle = sceneName,
                Quality = bookFile.Quality,
                ReleaseGroup = bookFile.ReleaseGroup
            };

            var input = new CustomFormatInput
            {
                BookInfo = bookInfo,
                Author = author,
                Size = bookFile.Size,
                Filename = Path.GetFileName(bookFile.Path)
            };

            return ParseCustomFormat(input, allCustomFormats);
        }
    }
}
