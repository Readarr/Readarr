using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Core.Music;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.TrackImport.Identification
{
    public static class DistanceCalculator
    {
        private static readonly Logger Logger = NzbDroneLogger.GetLogger(typeof(DistanceCalculator));

        public static readonly List<string> VariousAuthorIds = new List<string> { "89ad4ac3-39f7-470e-963a-56509c546377" };
        private static readonly List<string> VariousArtistNames = new List<string> { "various artists", "various", "va", "unknown" };
        private static readonly List<IsoCountry> PreferredCountries = new List<string>
        {
            "United States",
            "United Kingdom",
            "Europe",
            "[Worldwide]"
        }.Select(x => IsoCountries.Find(x)).ToList();

        private static readonly RegexReplace StripSeriesRegex = new RegexReplace(@"\([^\)].+?\)$", string.Empty, RegexOptions.Compiled);

        public static Distance BookDistance(List<LocalTrack> localTracks, Book release)
        {
            var dist = new Distance();

            var artists = new List<string> { localTracks.MostCommon(x => x.FileTrackInfo.ArtistTitle) ?? "" };

            // Add version based on un-reversed
            if (artists[0].Contains(','))
            {
                artists.Add(artists[0].Split(',').Select(x => x.Trim()).Reverse().ConcatToString(" "));
            }

            dist.AddString("artist", artists, release.AuthorMetadata.Value.Name);
            Logger.Trace("artist: '{0}' vs '{1}'; {2}", artists.ConcatToString("' or '"), release.AuthorMetadata.Value.Name, dist.NormalizedDistance());

            var title = localTracks.MostCommon(x => x.FileTrackInfo.AlbumTitle) ?? "";
            var titleOptions = new List<string> { release.Title };
            if (titleOptions[0].Contains("#"))
            {
                titleOptions.Add(StripSeriesRegex.Replace(titleOptions[0]));
            }

            if (release.SeriesLinks?.Value?.Any() ?? false)
            {
                foreach (var l in release.SeriesLinks.Value)
                {
                    titleOptions.Add($"{l.Series.Value.Title} {l.Position} {release.Title}");
                    titleOptions.Add($"{release.Title} {l.Series.Value.Title} {l.Position}");
                }
            }

            dist.AddString("album", title, titleOptions);
            Logger.Trace("album: '{0}' vs '{1}'; {2}", title, titleOptions.ConcatToString("' or '"), dist.NormalizedDistance());

            // Year
            var localYear = localTracks.MostCommon(x => x.FileTrackInfo.Year);
            if (localYear > 0 && release.ReleaseDate.HasValue)
            {
                var albumYear = release.ReleaseDate?.Year ?? 0;
                if (localYear == albumYear)
                {
                    dist.Add("year", 0.0);
                }
                else
                {
                    var remoteYear = albumYear;
                    var diff = Math.Abs(localYear - remoteYear);
                    var diff_max = Math.Abs(DateTime.Now.Year - remoteYear);
                    dist.AddRatio("year", diff, diff_max);
                }

                Logger.Trace($"year: {localYear} vs {release.ReleaseDate?.Year}; {dist.NormalizedDistance()}");
            }

            return dist;
        }
    }
}
