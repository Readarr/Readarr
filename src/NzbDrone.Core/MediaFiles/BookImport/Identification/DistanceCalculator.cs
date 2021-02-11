using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Core.Books;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.BookImport.Identification
{
    public static class DistanceCalculator
    {
        private static readonly Logger Logger = NzbDroneLogger.GetLogger(typeof(DistanceCalculator));

        public static readonly List<string> VariousAuthorIds = new List<string> { "89ad4ac3-39f7-470e-963a-56509c546377" };
        private static readonly List<string> VariousAuthorNames = new List<string> { "various authors", "various", "va", "unknown" };
        private static readonly List<IsoCountry> PreferredCountries = new List<string>
        {
            "United States",
            "United Kingdom",
            "Europe",
            "[Worldwide]"
        }.Select(x => IsoCountries.Find(x)).ToList();

        private static readonly RegexReplace StripSeriesRegex = new RegexReplace(@"\([^\)].+?\)$", string.Empty, RegexOptions.Compiled);

        public static Distance BookDistance(List<LocalBook> localTracks, Edition edition)
        {
            var dist = new Distance();

            var authors = new List<string> { localTracks.MostCommon(x => x.FileTrackInfo.AuthorTitle) ?? "" };

            // Add version based on un-reversed
            if (authors[0].Contains(','))
            {
                authors.Add(authors[0].Split(',').Select(x => x.Trim()).Reverse().ConcatToString(" "));
            }

            dist.AddString("author", authors, edition.Book.Value.AuthorMetadata.Value.Name);
            Logger.Trace("author: '{0}' vs '{1}'; {2}", authors.ConcatToString("' or '"), edition.Book.Value.AuthorMetadata.Value.Name, dist.NormalizedDistance());

            var title = localTracks.MostCommon(x => x.FileTrackInfo.BookTitle) ?? "";
            var titleOptions = new List<string> { edition.Title, edition.Book.Value.Title };
            if (titleOptions[0].Contains("#"))
            {
                titleOptions.Add(StripSeriesRegex.Replace(titleOptions[0]));
            }

            if (edition.Book.Value.SeriesLinks?.Value?.Any() ?? false)
            {
                foreach (var l in edition.Book.Value.SeriesLinks.Value)
                {
                    if (l.Series?.Value?.Title?.IsNotNullOrWhiteSpace() ?? false)
                    {
                        titleOptions.Add($"{l.Series.Value.Title} {l.Position} {edition.Title}");
                        titleOptions.Add($"{edition.Title} {l.Series.Value.Title} {l.Position}");
                    }
                }
            }

            dist.AddString("book", title, titleOptions);
            Logger.Trace("book: '{0}' vs '{1}'; {2}", title, titleOptions.ConcatToString("' or '"), dist.NormalizedDistance());

            // Year
            var localYear = localTracks.MostCommon(x => x.FileTrackInfo.Year);
            if (localYear > 0 && edition.ReleaseDate.HasValue)
            {
                var bookYear = edition.ReleaseDate?.Year ?? 0;
                if (localYear == bookYear)
                {
                    dist.Add("year", 0.0);
                }
                else
                {
                    var remoteYear = bookYear;
                    var diff = Math.Abs(localYear - remoteYear);
                    var diff_max = Math.Abs(DateTime.Now.Year - remoteYear);
                    dist.AddRatio("year", diff, diff_max);
                }

                Logger.Trace($"year: {localYear} vs {edition.ReleaseDate?.Year}; {dist.NormalizedDistance()}");
            }

            return dist;
        }
    }
}
