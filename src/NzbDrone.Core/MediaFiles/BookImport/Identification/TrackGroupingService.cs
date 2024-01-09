using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NLog;
using NzbDrone.Common.Disk;
using NzbDrone.Common.Extensions;
using NzbDrone.Common.Instrumentation;
using NzbDrone.Common.Instrumentation.Extensions;
using NzbDrone.Core.Parser.Model;

namespace NzbDrone.Core.MediaFiles.BookImport.Identification
{
    public interface ITrackGroupingService
    {
        List<LocalEdition> GroupTracks(List<LocalBook> localTracks);
    }

    public class TrackGroupingService : ITrackGroupingService
    {
        private static readonly Logger _logger = NzbDroneLogger.GetLogger(typeof(TrackGroupingService));

        private static readonly List<string> MultiDiscMarkers = new () { @"dis[ck]", @"cd" };
        private static readonly string MultiDiscPatternFormat = @"^(?<root>.*%s[\W_]*)\d";
        private static readonly List<string> VariousAuthorTitles = new () { "", "various authors", "various", "va", "unknown" };

        public List<LocalEdition> GroupTracks(List<LocalBook> localTracks)
        {
            _logger.ProgressInfo($"Grouping {localTracks.Count} tracks");

            var releases = new List<LocalEdition>();

            // text files are always single file releases
            var textFiles = localTracks.Where(x => MediaFileExtensions.TextExtensions.Contains(Path.GetExtension(x.Path))).ToList();

            foreach (var file in textFiles)
            {
                releases.Add(new LocalEdition(new List<LocalBook> { file }));
            }

            // first attempt, assume grouped by folder
            var unprocessed = new List<LocalBook>();
            foreach (var group in GroupTracksByDirectory(localTracks.Except(textFiles).ToList()))
            {
                var tracks = group.ToList();
                if (LooksLikeSingleRelease(tracks))
                {
                    releases.Add(new LocalEdition(tracks));
                }
                else
                {
                    unprocessed.AddRange(tracks);
                }
            }

            // If anything didn't get grouped correctly, try grouping by Book (to pick up VA)
            var unprocessed2 = new List<LocalBook>();
            foreach (var group in unprocessed.GroupBy(x => x.FileTrackInfo.BookTitle))
            {
                _logger.Debug("Falling back to grouping by book tag");
                var tracks = group.ToList();
                if (LooksLikeSingleRelease(tracks))
                {
                    releases.Add(new LocalEdition(tracks));
                }
                else
                {
                    unprocessed2.AddRange(tracks);
                }
            }

            // Finally fall back to grouping by Book/Author pair
            foreach (var group in unprocessed2.GroupBy(x => new { x.FileTrackInfo.AuthorTitle, x.FileTrackInfo.BookTitle }))
            {
                _logger.Debug("Falling back to grouping by book+author tag");
                releases.Add(new LocalEdition(group.ToList()));
            }

            return releases;
        }

        private static bool HasCommonEntry(IEnumerable<string> values, double threshold, double fuzz)
        {
            var groups = values.GroupBy(x => x).OrderByDescending(x => x.Count());
            var distinctCount = groups.Count();
            var mostCommonCount = groups.First().Count();
            var mostCommonEntry = groups.First().Key;
            var totalCount = values.Count();

            // merge groups that are close to the most common value
            foreach (var group in groups.Skip(1))
            {
                if (mostCommonEntry.IsNotNullOrWhiteSpace() &&
                    group.Key.IsNotNullOrWhiteSpace() &&
                    mostCommonEntry.LevenshteinCoefficient(group.Key) > fuzz)
                {
                    distinctCount--;
                    mostCommonCount += group.Count();
                }
            }

            _logger.Trace($"DistinctCount {distinctCount} MostCommonCount {mostCommonCount} TotalCout {totalCount}");

            if (distinctCount > 1 &&
                (distinctCount / (double)totalCount > threshold ||
                 mostCommonCount / (double)totalCount < 1 - threshold))
            {
                return false;
            }

            return true;
        }

        public static bool LooksLikeSingleRelease(List<LocalBook> tracks)
        {
            // returns true if we think all the tracks belong to a single release

            // author/book tags must be the same for 75% of tracks, with no more than 25% having different values
            // (except in the case of various authors)
            const double bookTagThreshold = 0.25;
            const double authorTagThreshold = 0.25;
            const double tagFuzz = 0.9;

            // check that any Book/Release MBID is unique
            if (tracks.Select(x => x.FileTrackInfo.BookMBId).Distinct().Count(x => x.IsNotNullOrWhiteSpace()) > 1 ||
                tracks.Select(x => x.FileTrackInfo.ReleaseMBId).Distinct().Count(x => x.IsNotNullOrWhiteSpace()) > 1)
            {
                _logger.Trace("LooksLikeSingleRelease: MBIDs are not unique");
                return false;
            }

            // check that there's a common book tag.
            var bookTags = tracks.Select(x => x.FileTrackInfo.BookTitle);
            if (!HasCommonEntry(bookTags, bookTagThreshold, tagFuzz))
            {
                _logger.Trace("LooksLikeSingleRelease: No common book tag");
                return false;
            }

            // If not various authors, make sure authors are sensible
            if (!IsVariousAuthors(tracks))
            {
                var authorTags = tracks.Select(x => x.FileTrackInfo.AuthorTitle);
                if (!HasCommonEntry(authorTags, authorTagThreshold, tagFuzz))
                {
                    _logger.Trace("LooksLikeSingleRelease: No common author tag");
                    return false;
                }
            }

            return true;
        }

        public static bool IsVariousAuthors(List<LocalBook> tracks)
        {
            // checks whether most common title is a known VA title
            // Also checks whether more than 75% of tracks have a distinct author and that the most common author
            // is responsible for < 25% of tracks
            const double authorTagThreshold = 0.75;
            const double tagFuzz = 0.9;

            var authorTags = tracks.Select(x => x.FileTrackInfo.AuthorTitle).ToList();

            if (!HasCommonEntry(authorTags, authorTagThreshold, tagFuzz))
            {
                return true;
            }

            if (VariousAuthorTitles.Contains(authorTags.GroupBy(x => x).OrderByDescending(x => x.Count()).First().Key, StringComparer.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private IEnumerable<List<LocalBook>> GroupTracksByDirectory(List<LocalBook> tracks)
        {
            // we want to check for layouts like:
            // xx/CD1/1.mp3
            // xx/CD2/1.mp3
            // or
            // yy Disc 1/1.mp3
            // yy Disc 2/1.mp3
            // and group them.

            // we only bother doing this for the immediate parent directory.
            var trackFolders = tracks.Select(x => Tuple.Create(x, Path.GetDirectoryName(x.Path))).ToList();

            var distinctFolders = trackFolders.Select(x => x.Item2).Distinct().ToList();
            distinctFolders.Sort();

            _logger.Trace("Folders:\n{0}", string.Join("\n", distinctFolders));

            Regex subdirRegex = null;
            var output = new List<LocalBook>();
            foreach (var folder in distinctFolders)
            {
                if (subdirRegex != null)
                {
                    if (subdirRegex.IsMatch(folder))
                    {
                        // current folder continues match, so append output
                        output.AddRange(tracks.Where(x => x.Path.StartsWith(folder)));
                        continue;
                    }
                }

                // we have finished a multi disc match.  yield the previous output
                // and check current folder
                if (output.Count > 0)
                {
                    _logger.Trace("Yielding from 1:\n{0}", string.Join("\n", output));
                    yield return output;

                    output = new List<LocalBook>();
                }

                // reset and put current folder into output
                subdirRegex = null;
                var currentTracks = trackFolders.Where(x => x.Item2.Equals(folder, DiskProviderBase.PathStringComparison))
                    .Select(x => x.Item1);
                output.AddRange(currentTracks);

                // check if the start of another multi disc match
                foreach (var marker in MultiDiscMarkers)
                {
                    // check if this is the first of a multi-disc set of folders
                    var pattern = MultiDiscPatternFormat.Replace("%s", marker);
                    var multiStartRegex = new Regex(pattern, RegexOptions.IgnoreCase);

                    var match = multiStartRegex.Match(folder);
                    if (match.Success)
                    {
                        var subdirPattern = $"^{Regex.Escape(match.Groups["root"].ToString())}\\d+$";
                        subdirRegex = new Regex(subdirPattern, RegexOptions.IgnoreCase);
                        break;
                    }
                }

                if (subdirRegex == null)
                {
                    // not the start of a multi-disc match, yield
                    _logger.Trace("Yielding from 2:\n{0}", string.Join("\n", output));
                    yield return output;

                    // reset output
                    output = new List<LocalBook>();
                }
            }

            // return the final stored output
            if (output.Count > 0)
            {
                _logger.Trace("Yielding final:\n{0}", string.Join("\n", output));
                yield return output;
            }
        }
    }
}
