using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Profiles.Qualities;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.Qualities
{
    [TestFixture]
    public class QualityFixture : CoreTest
    {
        public static object[] FromIntCases =
                {
                        new object[] { 0,  Quality.Unknown },
                        new object[] { 1,  Quality.PDF },
                        new object[] { 2,  Quality.MOBI },
                        new object[] { 3,  Quality.EPUB },
                        new object[] { 4,  Quality.AZW3 },
                        new object[] { 10, Quality.MP3_320 },
                        new object[] { 11, Quality.FLAC },
                        new object[] { 12, Quality.ALAC },
                        new object[] { 13, Quality.MP1 },
                        new object[] { 14, Quality.MP2 },
                        new object[] { 15, Quality.MP3VBR },
                        new object[] { 16, Quality.MP3CBR },
                        new object[] { 17, Quality.APE },
                        new object[] { 18, Quality.WMA },
                        new object[] { 19, Quality.WAV },
                        new object[] { 20, Quality.WAVPACK },
                        new object[] { 21, Quality.AAC },
                        new object[] { 22, Quality.AACVBR },
                        new object[] { 23, Quality.OGG },
                        new object[] { 24, Quality.OPUS },
                };

        public static object[] ToIntCases =
                {
                        new object[] { Quality.Unknown, 0 },
                        new object[] { Quality.PDF,     1 },
                        new object[] { Quality.MOBI,    2 },
                        new object[] { Quality.EPUB,    3 },
                        new object[] { Quality.AZW3,    4 },
                        new object[] { Quality.MP3_320, 10 },
                        new object[] { Quality.FLAC,    11 },
                        new object[] { Quality.ALAC,    12 },
                        new object[] { Quality.MP1,     13 },
                        new object[] { Quality.MP2,     14 },
                        new object[] { Quality.MP3VBR,  15 },
                        new object[] { Quality.MP3CBR,  16 },
                        new object[] { Quality.APE,     17 },
                        new object[] { Quality.WMA,     18 },
                        new object[] { Quality.WAV,     19 },
                        new object[] { Quality.WAVPACK, 20 },
                        new object[] { Quality.AAC,     21 },
                        new object[] { Quality.AACVBR,  22 },
                        new object[] { Quality.OGG,     23 },
                        new object[] { Quality.OPUS,    24 },
                };

        [Test]
        [TestCaseSource(nameof(FromIntCases))]
        public void should_be_able_to_convert_int_to_qualityTypes(int source, Quality expected)
        {
            var quality = (Quality)source;
            quality.Should().Be(expected);
        }

        [Test]
        [TestCaseSource(nameof(ToIntCases))]
        public void should_be_able_to_convert_qualityTypes_to_int(Quality source, int expected)
        {
            var i = (int)source;
            i.Should().Be(expected);
        }

        public static List<QualityProfileQualityItem> GetDefaultQualities(params Quality[] allowed)
        {
            var qualities = new List<Quality>
            {
                Quality.Unknown,
                Quality.MOBI,
                Quality.EPUB,
                Quality.AZW3,
                Quality.MP3_320,
                Quality.FLAC,
                Quality.ALAC,
                Quality.MP1,
                Quality.MP2,
                Quality.MP3VBR,
                Quality.MP3CBR,
                Quality.APE,
                Quality.WMA,
                Quality.WAV,
                Quality.WAVPACK,
                Quality.AAC,
                Quality.AACVBR,
                Quality.OGG,
                Quality.OPUS
            };

            if (allowed.Length == 0)
            {
                allowed = qualities.ToArray();
            }

            var items = qualities
                .Except(allowed)
                .Concat(allowed)
                .Select(v => new QualityProfileQualityItem { Quality = v, Allowed = allowed.Contains(v) }).ToList();

            return items;
        }
    }
}
