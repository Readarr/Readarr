using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.EnsureThat;
using NzbDrone.Common.Extensions;
using NzbDrone.Core.Books;
using NzbDrone.Core.CustomFormats;
using NzbDrone.Core.MediaFiles;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Qualities;

namespace NzbDrone.Core.Organizer
{
    public interface IBuildFileNames
    {
        string BuildBookFileName(Author author, Edition edition, BookFile bookFile, NamingConfig namingConfig = null, List<CustomFormat> customFormats = null);
        string BuildBookFilePath(Author author, Edition edition, string fileName, string extension);
        string BuildBookPath(Author author);
        BasicNamingConfig GetBasicNamingConfig(NamingConfig nameSpec);
        string GetAuthorFolder(Author author, NamingConfig namingConfig = null);
    }

    public class FileNameBuilder : IBuildFileNames
    {
        private readonly INamingConfigService _namingConfigService;
        private readonly IQualityDefinitionService _qualityDefinitionService;
        private readonly ICustomFormatCalculationService _formatCalculator;
        private readonly ICached<BookFormat[]> _trackFormatCache;
        private readonly Logger _logger;

        private static readonly Regex TitleRegex = new Regex(@"\{(?<prefix>[- ._\[(]*)(?<token>(?:[a-z0-9]+)(?:(?<separator>[- ._]+)(?:[a-z0-9]+))?)(?::(?<customFormat>[a-z0-9]+))?(?<suffix>[- ._)\]]*)\}",
                                                             RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex PartRegex = new Regex(@"\{(?<prefix>[^{]*?)(?<token1>PartNumber|PartCount)(?::(?<customFormat1>[a-z0-9]+))?(?<separator>.*(?=PartNumber|PartCount))?((?<token2>PartNumber|PartCount)(?::(?<customFormat2>[a-z0-9]+))?)?(?<suffix>[^}]*)\}",
                                                            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex SeasonEpisodePatternRegex = new Regex(@"(?<separator>(?<=})[- ._]+?)?(?<seasonEpisode>s?{season(?:\:0+)?}(?<episodeSeparator>[- ._]?[ex])(?<episode>{episode(?:\:0+)?}))(?<separator>[- ._]+?(?={))?",
                                                                            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex AuthorNameRegex = new Regex(@"(?<token>\{(?:Author)(?<separator>[- ._])(Clean)?(Sort)?Name(The)?\})",
                                                                            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static readonly Regex BookTitleRegex = new Regex(@"(?<token>\{(?:Book)(?<separator>[- ._])(Clean)?Title(The)?(NoSub)?\})",
                                                                            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex FileNameCleanupRegex = new Regex(@"([- ._])(\1)+", RegexOptions.Compiled);
        private static readonly Regex TrimSeparatorsRegex = new Regex(@"[- ._]$", RegexOptions.Compiled);

        private static readonly Regex ScenifyRemoveChars = new Regex(@"(?<=\s)(,|<|>|\/|\\|;|:|'|""|\||`|~|!|\?|@|$|%|^|\*|-|_|=){1}(?=\s)|('|:|\?|,)(?=(?:(?:s|m)\s)|\s|$)|(\(|\)|\[|\]|\{|\})", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex ScenifyReplaceChars = new Regex(@"[\/]", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex TitlePrefixRegex = new Regex(@"^(The|An|A) (.*?)((?: *\([^)]+\))*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public FileNameBuilder(INamingConfigService namingConfigService,
                               IQualityDefinitionService qualityDefinitionService,
                               ICacheManager cacheManager,
                               ICustomFormatCalculationService formatCalculator,
                               Logger logger)
        {
            _namingConfigService = namingConfigService;
            _qualityDefinitionService = qualityDefinitionService;
            _formatCalculator = formatCalculator;
            _trackFormatCache = cacheManager.GetCache<BookFormat[]>(GetType(), "bookFormat");
            _logger = logger;
        }

        public string BuildBookFileName(Author author, Edition edition, BookFile bookFile, NamingConfig namingConfig = null, List<CustomFormat> customFormats = null)
        {
            if (namingConfig == null)
            {
                namingConfig = _namingConfigService.GetConfig();
            }

            if (!namingConfig.RenameBooks)
            {
                return GetOriginalFileName(bookFile);
            }

            if (namingConfig.StandardBookFormat.IsNullOrWhiteSpace())
            {
                throw new NamingFormatException("File name format cannot be empty");
            }

            var pattern = namingConfig.StandardBookFormat;

            var tokenHandlers = new Dictionary<string, Func<TokenMatch, string>>(FileNameBuilderTokenEqualityComparer.Instance);

            AddAuthorTokens(tokenHandlers, author);
            AddBookTokens(tokenHandlers, edition);
            AddBookFileTokens(tokenHandlers, bookFile);
            AddQualityTokens(tokenHandlers, author, bookFile);
            AddMediaInfoTokens(tokenHandlers, bookFile);
            AddCustomFormats(tokenHandlers, author, bookFile, customFormats);

            var splitPatterns = pattern.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            var components = new List<string>();

            foreach (var s in splitPatterns)
            {
                var splitPattern = s;

                var component = ReplacePartTokens(splitPattern, tokenHandlers, namingConfig).Trim();
                component = ReplaceTokens(component, tokenHandlers, namingConfig).Trim();

                component = FileNameCleanupRegex.Replace(component, match => match.Captures[0].Value[0].ToString());
                component = TrimSeparatorsRegex.Replace(component, string.Empty);

                if (component.IsNotNullOrWhiteSpace())
                {
                    components.Add(component);
                }
            }

            return Path.Combine(components.ToArray());
        }

        public string BuildBookFilePath(Author author, Edition edition, string fileName, string extension)
        {
            Ensure.That(extension, () => extension).IsNotNullOrWhiteSpace();

            var path = BuildBookPath(author);

            return Path.Combine(path, fileName + extension);
        }

        public string BuildBookPath(Author author)
        {
            return author.Path;
        }

        public BasicNamingConfig GetBasicNamingConfig(NamingConfig nameSpec)
        {
            var trackFormat = GetTrackFormat(nameSpec.StandardBookFormat).LastOrDefault();

            if (trackFormat == null)
            {
                return new BasicNamingConfig();
            }

            var basicNamingConfig = new BasicNamingConfig
            {
                Separator = trackFormat.Separator
            };

            var titleTokens = TitleRegex.Matches(nameSpec.StandardBookFormat);

            foreach (Match match in titleTokens)
            {
                var separator = match.Groups["separator"].Value;
                var token = match.Groups["token"].Value;

                if (!separator.Equals(" "))
                {
                    basicNamingConfig.ReplaceSpaces = true;
                }

                if (token.StartsWith("{Author", StringComparison.InvariantCultureIgnoreCase))
                {
                    basicNamingConfig.IncludeAuthorName = true;
                }

                if (token.StartsWith("{Book", StringComparison.InvariantCultureIgnoreCase))
                {
                    basicNamingConfig.IncludeBookTitle = true;
                }

                if (token.StartsWith("{Quality", StringComparison.InvariantCultureIgnoreCase))
                {
                    basicNamingConfig.IncludeQuality = true;
                }
            }

            return basicNamingConfig;
        }

        public string GetAuthorFolder(Author author, NamingConfig namingConfig = null)
        {
            if (namingConfig == null)
            {
                namingConfig = _namingConfigService.GetConfig();
            }

            var pattern = namingConfig.AuthorFolderFormat;
            var tokenHandlers = new Dictionary<string, Func<TokenMatch, string>>(FileNameBuilderTokenEqualityComparer.Instance);

            AddAuthorTokens(tokenHandlers, author);

            var splitPatterns = pattern.Split(new char[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
            var components = new List<string>();

            foreach (var s in splitPatterns)
            {
                var splitPattern = s;

                var component = ReplaceTokens(splitPattern, tokenHandlers, namingConfig);
                component = CleanFolderName(component);

                if (component.IsNotNullOrWhiteSpace())
                {
                    components.Add(component);
                }
            }

            return Path.Combine(components.ToArray());
        }

        public static string CleanTitle(string title)
        {
            title = title.Replace("&", "and");
            title = ScenifyReplaceChars.Replace(title, " ");
            title = ScenifyRemoveChars.Replace(title, string.Empty);

            return title;
        }

        public static string TitleThe(string title)
        {
            return TitlePrefixRegex.Replace(title, "$2, $1$3");
        }

        public static string CleanFileName(string name)
        {
            return CleanFileName(name, NamingConfig.Default);
        }

        public static string CleanFolderName(string name)
        {
            name = FileNameCleanupRegex.Replace(name, match => match.Captures[0].Value[0].ToString());

            return name.Trim(' ', '.');
        }

        private void AddAuthorTokens(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, Author author)
        {
            tokenHandlers["{Author Name}"] = m => author.Name;
            tokenHandlers["{Author CleanName}"] = m => CleanTitle(author.Name);
            tokenHandlers["{Author NameThe}"] = m => TitleThe(author.Name);
            tokenHandlers["{Author SortName}"] = m => author?.Metadata?.Value?.NameLastFirst ?? string.Empty;
            tokenHandlers["{Author NameFirstCharacter}"] = m => TitleThe(author.Name).Substring(0, 1).FirstCharToUpper();

            if (author.Metadata.Value.Disambiguation != null)
            {
                tokenHandlers["{Author Disambiguation}"] = m => author.Metadata.Value.Disambiguation;
            }
        }

        private void AddBookTokens(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, Edition edition)
        {
            tokenHandlers["{Book Title}"] = m => edition.Title;
            tokenHandlers["{Book CleanTitle}"] = m => CleanTitle(edition.Title);
            tokenHandlers["{Book TitleThe}"] = m => TitleThe(edition.Title);

            var (titleNoSub, subtitle) = edition.Title.SplitBookTitle(edition.Book.Value.AuthorMetadata.Value.Name);

            tokenHandlers["{Book TitleNoSub}"] = m => titleNoSub;
            tokenHandlers["{Book CleanTitleNoSub}"] = m => CleanTitle(titleNoSub);
            tokenHandlers["{Book TitleTheNoSub}"] = m => TitleThe(titleNoSub);

            tokenHandlers["{Book Subtitle}"] = m => subtitle;
            tokenHandlers["{Book CleanSubtitle}"] = m => CleanTitle(subtitle);
            tokenHandlers["{Book SubtitleThe}"] = m => TitleThe(subtitle);

            var seriesLinks = edition.Book.Value.SeriesLinks.Value;
            if (seriesLinks.Any())
            {
                var primarySeries = seriesLinks.OrderBy(x => x.SeriesPosition).First();
                var seriesTitle = primarySeries.Series?.Value?.Title + (primarySeries.Position.IsNotNullOrWhiteSpace() ? $" #{primarySeries.Position}" : string.Empty);

                tokenHandlers["{Book Series}"] = m => primarySeries.Series.Value.Title;
                tokenHandlers["{Book SeriesPosition}"] = m => primarySeries.Position;
                tokenHandlers["{Book SeriesTitle}"] = m => seriesTitle;
            }

            if (edition.Disambiguation != null)
            {
                tokenHandlers["{Book Disambiguation}"] = m => edition.Disambiguation;
            }

            if (edition.ReleaseDate.HasValue)
            {
                tokenHandlers["{Release Year}"] = m => edition.ReleaseDate.Value.Year.ToString();
            }
            else if (edition.Book.Value.ReleaseDate.HasValue)
            {
                tokenHandlers["{Release Year}"] = m => edition.Book.Value.ReleaseDate.Value.Year.ToString();
            }
            else
            {
                tokenHandlers["{Release Year}"] = m => "Unknown";
            }

            if (edition.ReleaseDate.HasValue)
            {
                tokenHandlers["{Edition Year}"] = m => edition.ReleaseDate.Value.Year.ToString();
            }
            else
            {
                tokenHandlers["{Edition Year}"] = m => "Unknown";
            }

            if (edition.Book.Value.ReleaseDate.HasValue)
            {
                tokenHandlers["{Release YearFirst}"] = m => edition.Book.Value.ReleaseDate.Value.Year.ToString();
            }
            else
            {
                tokenHandlers["{Release YearFirst}"] = m => "Unknown";
            }
        }

        private void AddBookFileTokens(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, BookFile bookFile)
        {
            tokenHandlers["{Original Title}"] = m => GetOriginalTitle(bookFile);
            tokenHandlers["{Original Filename}"] = m => GetOriginalFileName(bookFile);
            tokenHandlers["{Release Group}"] = m => bookFile.ReleaseGroup ?? m.DefaultValue("Readarr");

            if (bookFile.PartCount > 1)
            {
                tokenHandlers["{PartNumber}"] = m => bookFile.Part.ToString(m.CustomFormat);
                tokenHandlers["{PartCount}"] = m => bookFile.PartCount.ToString(m.CustomFormat);
            }
        }

        private void AddQualityTokens(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, Author author, BookFile bookFile)
        {
            var qualityTitle = _qualityDefinitionService.Get(bookFile.Quality.Quality).Title;
            var qualityProper = GetQualityProper(bookFile.Quality);

            //var qualityReal = GetQualityReal(author, bookFile.Quality);
            tokenHandlers["{Quality Full}"] = m => string.Format("{0}", qualityTitle);
            tokenHandlers["{Quality Title}"] = m => qualityTitle;
            tokenHandlers["{Quality Proper}"] = m => qualityProper;

            //tokenHandlers["{Quality Real}"] = m => qualityReal;
        }

        private void AddMediaInfoTokens(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, BookFile bookFile)
        {
            if (bookFile.MediaInfo == null)
            {
                _logger.Trace("Media info is unavailable for {0}", bookFile);

                return;
            }

            var audioCodec = MediaInfoFormatter.FormatAudioCodec(bookFile.MediaInfo);
            var audioChannels = MediaInfoFormatter.FormatAudioChannels(bookFile.MediaInfo);
            var audioChannelsFormatted = audioChannels > 0 ?
                                audioChannels.ToString("F1", CultureInfo.InvariantCulture) :
                                string.Empty;

            tokenHandlers["{MediaInfo AudioCodec}"] = m => audioCodec;
            tokenHandlers["{MediaInfo AudioChannels}"] = m => audioChannelsFormatted;
            tokenHandlers["{MediaInfo AudioBitRate}"] = m => MediaInfoFormatter.FormatAudioBitrate(bookFile.MediaInfo);
            tokenHandlers["{MediaInfo AudioBitsPerSample}"] = m => MediaInfoFormatter.FormatAudioBitsPerSample(bookFile.MediaInfo);
            tokenHandlers["{MediaInfo AudioSampleRate}"] = m => MediaInfoFormatter.FormatAudioSampleRate(bookFile.MediaInfo);
        }

        private void AddCustomFormats(Dictionary<string, Func<TokenMatch, string>> tokenHandlers, Author author, BookFile bookFile, List<CustomFormat> customFormats = null)
        {
            if (customFormats == null)
            {
                bookFile.Author = author;
                customFormats = _formatCalculator.ParseCustomFormat(bookFile, author);
            }

            tokenHandlers["{Custom Formats}"] = m => string.Join(" ", customFormats.Where(x => x.IncludeCustomFormatWhenRenaming));
        }

        private string ReplaceTokens(string pattern, Dictionary<string, Func<TokenMatch, string>> tokenHandlers, NamingConfig namingConfig)
        {
            return TitleRegex.Replace(pattern, match => ReplaceToken(match, tokenHandlers, namingConfig));
        }

        private string ReplaceToken(Match match, Dictionary<string, Func<TokenMatch, string>> tokenHandlers, NamingConfig namingConfig)
        {
            var tokenMatch = new TokenMatch
            {
                RegexMatch = match,
                Prefix = match.Groups["prefix"].Value,
                Separator = match.Groups["separator"].Value,
                Suffix = match.Groups["suffix"].Value,
                Token = match.Groups["token"].Value,
                CustomFormat = match.Groups["customFormat"].Value
            };

            if (tokenMatch.CustomFormat.IsNullOrWhiteSpace())
            {
                tokenMatch.CustomFormat = null;
            }

            var tokenHandler = tokenHandlers.GetValueOrDefault(tokenMatch.Token, m => string.Empty);

            var replacementText = tokenHandler(tokenMatch).Trim();

            if (tokenMatch.Token.All(t => !char.IsLetter(t) || char.IsLower(t)))
            {
                replacementText = replacementText.ToLower();
            }
            else if (tokenMatch.Token.All(t => !char.IsLetter(t) || char.IsUpper(t)))
            {
                replacementText = replacementText.ToUpper();
            }

            if (!tokenMatch.Separator.IsNullOrWhiteSpace())
            {
                replacementText = replacementText.Replace(" ", tokenMatch.Separator);
            }

            replacementText = CleanFileName(replacementText, namingConfig);

            if (!replacementText.IsNullOrWhiteSpace())
            {
                replacementText = tokenMatch.Prefix + replacementText + tokenMatch.Suffix;
            }

            return replacementText;
        }

        private string ReplacePartTokens(string pattern, Dictionary<string, Func<TokenMatch, string>> tokenHandlers, NamingConfig namingConfig)
        {
            return PartRegex.Replace(pattern, match => ReplacePartToken(match, tokenHandlers, namingConfig));
        }

        private string ReplacePartToken(Match match, Dictionary<string, Func<TokenMatch, string>> tokenHandlers, NamingConfig namingConfig)
        {
            var tokenHandler = tokenHandlers.GetValueOrDefault($"{{{match.Groups["token1"].Value}}}", m => string.Empty);

            var tokenText1 = tokenHandler(new TokenMatch { CustomFormat = match.Groups["customFormat1"].Success ? match.Groups["customFormat1"].Value : "0" });

            if (tokenText1 == string.Empty)
            {
                return string.Empty;
            }

            var prefix = match.Groups["prefix"].Value;

            var tokenText2 = string.Empty;

            var separator = match.Groups["separator"].Success ? match.Groups["separator"].Value : string.Empty;

            var suffix = match.Groups["suffix"].Value;

            if (match.Groups["token2"].Success)
            {
                tokenHandler = tokenHandlers.GetValueOrDefault($"{{{match.Groups["token2"].Value}}}", m => string.Empty);

                tokenText2 = tokenHandler(new TokenMatch { CustomFormat = match.Groups["customFormat2"].Success ? match.Groups["customFormat2"].Value : "0" });
            }

            return $"{prefix}{tokenText1}{separator}{tokenText2}{suffix}";
        }

        private BookFormat[] GetTrackFormat(string pattern)
        {
            return _trackFormatCache.Get(pattern, () => SeasonEpisodePatternRegex.Matches(pattern).OfType<Match>()
                .Select(match => new BookFormat
                {
                    BookSeparator = match.Groups["episodeSeparator"].Value,
                    Separator = match.Groups["separator"].Value,
                    BookPattern = match.Groups["episode"].Value,
                }).ToArray());
        }

        private string GetQualityProper(QualityModel quality)
        {
            if (quality.Revision.Version > 1)
            {
                if (quality.Revision.IsRepack)
                {
                    return "Repack";
                }

                return "Proper";
            }

            return string.Empty;
        }

        private string GetOriginalTitle(BookFile bookFile)
        {
            if (bookFile.SceneName.IsNullOrWhiteSpace())
            {
                return GetOriginalFileName(bookFile);
            }

            return bookFile.SceneName;
        }

        private string GetOriginalFileName(BookFile bookFile)
        {
            return Path.GetFileNameWithoutExtension(bookFile.Path);
        }

        private static string CleanFileName(string name, NamingConfig namingConfig)
        {
            var result = name;
            string[] badCharacters = { "\\", "/", "<", ">", "?", "*", "|", "\"" };
            string[] goodCharacters = { "+", "+", "", "", "!", "-", "", "" };

            if (namingConfig.ReplaceIllegalCharacters)
            {
                // Smart replaces a colon followed by a space with space dash space for a better appearance
                if (namingConfig.ColonReplacementFormat == ColonReplacementFormat.Smart)
                {
                    result = result.Replace(": ", " - ");
                    result = result.Replace(":", "-");
                }
                else
                {
                    var replacement = string.Empty;

                    switch (namingConfig.ColonReplacementFormat)
                    {
                        case ColonReplacementFormat.Dash:
                            replacement = "-";
                            break;
                        case ColonReplacementFormat.SpaceDash:
                            replacement = " -";
                            break;
                        case ColonReplacementFormat.SpaceDashSpace:
                            replacement = " - ";
                            break;
                    }

                    result = result.Replace(":", replacement);
                }
            }
            else
            {
                result = result.Replace(":", string.Empty);
            }

            for (var i = 0; i < badCharacters.Length; i++)
            {
                result = result.Replace(badCharacters[i], namingConfig.ReplaceIllegalCharacters ? goodCharacters[i] : string.Empty);
            }

            return result.TrimStart(' ', '.').TrimEnd(' ');
        }
    }

    internal sealed class TokenMatch
    {
        public Match RegexMatch { get; set; }
        public string Prefix { get; set; }
        public string Separator { get; set; }
        public string Suffix { get; set; }
        public string Token { get; set; }
        public string CustomFormat { get; set; }

        public string DefaultValue(string defaultValue)
        {
            if (string.IsNullOrEmpty(Prefix) && string.IsNullOrEmpty(Suffix))
            {
                return defaultValue;
            }
            else
            {
                return string.Empty;
            }
        }
    }

    public enum ColonReplacementFormat
    {
        Delete = 0,
        Dash = 1,
        SpaceDash = 2,
        SpaceDashSpace = 3,
        Smart = 4
    }
}
