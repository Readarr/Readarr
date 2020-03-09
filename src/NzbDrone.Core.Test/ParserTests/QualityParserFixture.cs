using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Qualities;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]

    public class QualityParserFixture : CoreTest
    {
        public static object[] SelfQualityParserCases =
        {
            new object[] { Quality.MP3_320 },
            new object[] { Quality.MP3_320 },
            new object[] { Quality.MP3_320 },
            new object[] { Quality.FLAC },
        };

        [TestCase("VA - The Best 101 Love Ballads (2017) MP3 [192 kbps]", null, 0)]
        [TestCase("ATCQ - The Love Movement 1998 2CD 192kbps  RIP", null, 0)]
        [TestCase("A Tribe Called Quest - The Love Movement 1998 2CD [192kbps] RIP", null, 0)]
        [TestCase("Maula - Jism 2 [2012] Mp3 - 192Kbps [Extended]- TK", null, 0)]
        [TestCase("VA - Complete Clubland - The Ultimate Ride Of Your Lfe [2014][MP3][192 kbps]", null, 0)]
        [TestCase("Complete Clubland - The Ultimate Ride Of Your Lfe [2014][MP3](192kbps)", null, 0)]
        [TestCase("The Ultimate Ride Of Your Lfe [192 KBPS][2014][MP3]", null, 0)]
        [TestCase("Gary Clark Jr - Live North America 2016 (2017) MP3 192kbps", null, 0)]
        [TestCase("Some Song [192][2014][MP3]", null, 0)]
        [TestCase("Other Song (192)[2014][MP3]", null, 0)]
        [TestCase("", "MPEG Version 1 Audio, Layer 3", 192)]
        [TestCase("Caetano Veloso Discografia Completa MP3 @256", null, 0)]
        [TestCase("Ricky Martin - A Quien Quiera Escuchar (2015) 256 kbps [GloDLS]", null, 0)]
        [TestCase("Jake Bugg - Jake Bugg (Album) [2012] {MP3 256 kbps}", null, 0)]
        [TestCase("Clean Bandit - New Eyes [2014] [Mp3-256]-V3nom [GLT]", null, 0)]
        [TestCase("Armin van Buuren - A State Of Trance 810 (20.04.2017) 256 kbps", null, 0)]
        [TestCase("PJ Harvey - Let England Shake [mp3-256-2011][trfkad]", null, 0)]
        [TestCase("", "MPEG Version 1 Audio, Layer 3", 256)]
        [TestCase("Beyoncé Lemonade [320] 2016 Beyonce Lemonade [320] 2016", null, 0)]
        [TestCase("Childish Gambino - Awaken, My Love Album 2016 mp3 320 Kbps", null, 0)]
        [TestCase("Maluma – Felices Los 4 MP3 320 Kbps 2017 Download", null, 0)]
        [TestCase("Ricardo Arjona - APNEA (Single 2014) (320 kbps)", null, 0)]
        [TestCase("Kehlani - SweetSexySavage (Deluxe Edition) (2017) 320", null, 0)]
        [TestCase("Anderson Paak - Malibu (320)(2016)", null, 0)]
        [TestCase("", "MPEG Version 1 Audio, Layer 3", 320)]
        [TestCase("Sia - This Is Acting (Standard Edition) [2016-Web-MP3-V0(VBR)]", null, 0)]
        [TestCase("Mount Eerie - A Crow Looked at Me (2017) [MP3 V0 VBR)]", null, 0)]
        [TestCase("", "MPEG Version 1 Audio, Layer 3 VBR", 298)]
        public void should_parse_mp3_quality(string title, string desc, int bitrate)
        {
            ParseAndVerifyQuality(title, desc, bitrate, Quality.MP3_320);
        }

        [TestCase("Kendrick Lamar - DAMN (2017) FLAC", null, 0)]
        [TestCase("Alicia Keys - Vault Playlist Vol. 1 (2017) [FLAC CD]", null, 0)]
        [TestCase("Gorillaz - Humanz (Deluxe) - lossless FLAC Tracks - 2017 - CDrip", null, 0)]
        [TestCase("David Bowie - Blackstar (2016) [FLAC]", null, 0)]
        [TestCase("The Cure - Greatest Hits (2001) FLAC Soup", null, 0)]
        [TestCase("Slowdive- Souvlaki (FLAC)", null, 0)]
        [TestCase("John Coltrane - Kulu Se Mama (1965) [EAC-FLAC]", null, 0)]
        [TestCase("The Rolling Stones - The Very Best Of '75-'94 (1995) {FLAC}", null, 0)]
        [TestCase("Migos-No_Label_II-CD-FLAC-2014-FORSAKEN", null, 0)]
        [TestCase("ADELE 25 CD FLAC 2015 PERFECT", null, 0)]
        [TestCase("", "Flac Audio", 1057)]
        public void should_parse_flac_quality(string title, string desc, int bitrate)
        {
            ParseAndVerifyQuality(title, desc, bitrate, Quality.FLAC);
        }

        [TestCase("Opus - Drums Unlimited (1966) [Flac]", null, 0)]
        public void should_not_parse_opus_quality(string title, string desc, int bitrate)
        {
            var result = QualityParser.ParseQuality(title);
            result.Quality.Should().Be(Quality.FLAC);
        }

        // Flack doesn't get match for 'FLAC' quality
        [TestCase("Roberta Flack 2006 - The Very Best of")]
        public void should_not_parse_flac_quality(string title)
        {
            ParseAndVerifyQuality(title, null, 0, Quality.Unknown);
        }

        [TestCase("The Chainsmokers & Coldplay - Something Just Like This")]
        [TestCase("Frank Ocean Blonde 2016")]

        //TODO: This should be parsed as Unknown and not MP3-96
        //[TestCase("A - NOW Thats What I Call Music 96 (2017) [Mp3~Kbps]")]
        [TestCase("Queen - The Ultimate Best Of Queen(2011)[mp3]")]
        [TestCase("Maroon 5 Ft Kendrick Lamar -Dont Wanna Know MP3 2016")]
        public void quality_parse(string title)
        {
            ParseAndVerifyQuality(title, null, 0, Quality.Unknown);
        }

        [Test]
        [TestCaseSource(nameof(SelfQualityParserCases))]
        public void parsing_our_own_quality_enum_name(Quality quality)
        {
            var fileName = string.Format("Some album [{0}]", quality.Name);
            var result = QualityParser.ParseQuality(fileName);
            result.Quality.Should().Be(quality);
        }

        [TestCase("Little Mix - Salute [Deluxe Edition] [2013] [M4A-256]-V3nom [GLT")]
        public void should_parse_quality_from_name(string title)
        {
            QualityParser.ParseQuality(title).QualityDetectionSource.Should().Be(QualityDetectionSource.Name);
        }

        [TestCase("01. Kanye West - Ultralight Beam.mp3")]
        [TestCase("01. Kanye West - Ultralight Beam.ogg")]

        //These get detected by name as we are looking for the extensions as identifiers for release names
        //[TestCase("01. Kanye West - Ultralight Beam.m4a")]
        //[TestCase("01. Kanye West - Ultralight Beam.wma")]
        //[TestCase("01. Kanye West - Ultralight Beam.wav")]
        public void should_parse_quality_from_extension(string title)
        {
            QualityParser.ParseQuality(title).QualityDetectionSource.Should().Be(QualityDetectionSource.Extension);
        }

        [Test]
        public void should_parse_null_quality_description_as_unknown()
        {
            QualityParser.ParseCodec(null, null).Should().Be(Codec.Unknown);
        }

        [TestCase("Artist Title - Album Title 2017 REPACK FLAC aAF", true)]
        [TestCase("Artist Title - Album Title 2017 RERIP FLAC aAF", true)]
        [TestCase("Artist Title - Album Title 2017 PROPER FLAC aAF", false)]
        public void should_be_able_to_parse_repack(string title, bool isRepack)
        {
            var result = QualityParser.ParseQuality(title);
            result.Revision.Version.Should().Be(2);
            result.Revision.IsRepack.Should().Be(isRepack);
        }

        private void ParseAndVerifyQuality(string name, string desc, int bitrate, Quality quality, int sampleSize = 0)
        {
            var result = QualityParser.ParseQuality(name);
            result.Quality.Should().Be(quality);
        }
    }
}
