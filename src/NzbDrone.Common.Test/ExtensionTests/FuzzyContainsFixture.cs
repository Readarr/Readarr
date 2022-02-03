using System.Collections.Generic;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Extensions;
using NzbDrone.Test.Common;

namespace NzbDrone.Common.Test
{
    [TestFixture]
    public class FuzzyContainsFixture : TestBase
    {
        [TestCase("abcdef", "abcdef", 0.5, 0)]
        [TestCase("", "abcdef", 0.5, -1)]
        [TestCase("abcdef", "", 0.5, -1)]
        [TestCase("", "", 0.5, -1)]
        [TestCase("abcdef", "de", 0.5, 3)]
        [TestCase("abcdef", "defy", 0.5, 3)]
        [TestCase("abcdef", "abcdefy", 0.5, 0)]
        [TestCase("I am the very model of a modern major general.", " that berry ", 0.3, 4)]
        [TestCase("abcdefghijk", "fgh", 0.5, 5)]
        [TestCase("abcdefghijk", "fgh", 0.5, 5)]
        [TestCase("abcdefghijk", "efxhi", 0.5, 4)]
        [TestCase("abcdefghijk", "cdefxyhijk", 0.5, 2)]
        [TestCase("abcdefghijk", "bxy", 0.5, -1)]
        [TestCase("123456789xx0", "3456789x0", 0.5, 2)]
        [TestCase("abcdef", "xxabc", 0.5, 0)]
        [TestCase("abcdef", "defyy", 0.5, 3)]
        [TestCase("abcdef", "xabcdefy", 0.5, 0)]
        [TestCase("abcdefghijk", "efxyhi", 0.6, 4)]
        [TestCase("abcdefghijk", "efxyhi", 0.7, -1)]
        [TestCase("abcdefghijk", "bcdef", 0.0, 1)]
        [TestCase("abcdexyzabcde", "abccde", 0.5, 0)]
        [TestCase("abcdefghijklmnopqrstuvwxyz", "abcdxxefg", 0.5, 0)]
        [TestCase("abcdefghijklmnopqrstuvwxyz", "abcdefg", 0.5, 0)]
        [TestCase("The quick brown fox jumps over the lazy dog", "The quick brown fox jumps over the lazy d", 0.5, 0)]
        [TestCase("The quick brown fox jumps over the lazy dog", "The quick brown fox jumps over the lazy g", 0.5, 0)]
        [TestCase("The quick brown fox jumps over the lazy dog", "quikc brown fox jumps over the lazy dog", 0.5, 4)]
        [TestCase("The quick brown fox jumps over the lazy dog", "qui jumps over the lazy dog", 0.5, 16)]
        [TestCase("The quick brown fox jumps over the lazy dog", "quikc brown fox jumps over the lazy dog", 0.5, 4)]
        [TestCase("u6IEytQiYpzAccsbjQ5ISuE4smDQ1ZiU42cFBrTeKB2XrVLEqAvgIiKlDP75iApy07jzmK", "xEytQiYpzAccsbjQ5ISuE4smDQ1ZiU42cFBrTeKB2XrVLEqAvgIiKlDP75iApy07jzmK", 0.5, 2)]
        [TestCase("plusifeelneedforredundantinformationintitlefield", "anthology", 0.5, -1)]
        public void FuzzyFind(string text, string pattern, double threshold, int expected)
        {
            text.FuzzyFind(pattern, threshold).Should().Be(expected);
        }

        [TestCase("abcdef", "abcdef", 1)]
        [TestCase("", "abcdef", 0)]
        [TestCase("abcdef", "", 0)]
        [TestCase("", "", 0)]
        [TestCase("abcdef", "de", 1)]
        [TestCase("abcdef", "defy", 0.75)]
        [TestCase("abcdef", "abcdefghk", 6.0 / 9)]
        [TestCase("abcdef", "zabcdefz", 6.0 / 8)]
        [TestCase("plusifeelneedforredundantinformationintitlefield", "anthology", 4.0 / 9)]
        [TestCase("+ (Plus) - I feel the need for redundant information in the title field", "+", 1)]
        public void FuzzyContains(string text, string pattern, double expectedScore)
        {
            text.FuzzyContains(pattern).Should().BeApproximately(expectedScore, 1e-9);
        }

        [TestCase("The quick brown fox jumps over the lazy dog", "The", " ", 0)]
        [TestCase("The quick brown fox jumps over the lazy dog", "over", " ", 26)]
        [TestCase("The quick brown fox jumps over the lazy dog", "dog", " ", 40)]
        public void should_find_exact_words(string text, string pattern, string delimiters, int expected)
        {
            var match = text.FuzzyMatch(pattern, 1, new HashSet<char>(delimiters));
            var result = match.Item1;

            result.Should().Be(expected);
        }

        [TestCase("The quick brown fox jumps over the lazy dog", "Th", " ")]
        [TestCase("The quick brown fox jumps over the lazy dog", "The q", " ")]
        [TestCase("The quick brown fox jumps over the lazy dog", "own", " ")]
        [TestCase("The quick brown fox jumps over the lazy dog", "brow", " ")]
        [TestCase("The quick brown fox jumps over the lazy dog", "og", " ")]
        [TestCase("The quick brown fox jumps over the lazy dog", "do", " ")]
        public void should_not_find_exact_matches_that_are_not_words(string text, string pattern, string delimiters)
        {
            var match = text.FuzzyMatch(pattern, 1, new HashSet<char>(delimiters));
            var result = match.Item1;

            result.Should().Be(-1);
        }

        [TestCase("The quick brown fox jumps over the lazy dog", "Th", " ", 0)]
        [TestCase("The quick brown fox jumps over the lazy dog", "Te", " ", 0)]
        [TestCase("The quick brown fox jumps over the lazy dog", "ovr", " ", 26)]
        [TestCase("The quick brown fox jumps over the lazy dog", "oveer", " ", 26)]
        [TestCase("The quick brown fox jumps over the lazy dog", "dog", " ", 40)]
        public void should_find_approximate_words(string text, string pattern, string delimiters, int expected)
        {
            var match = text.FuzzyMatch(pattern, 0.4, new HashSet<char>(delimiters));
            var result = match.Item1;

            result.Should().Be(expected);
        }

        [TestCase("The quick brown fox jumps over the lazy dog", "Th", " ", 0, 0.5)]
        [TestCase("The quick brown fox jumps over the lazy dog", "The q", " ", 0, 0.6)]
        [TestCase("The quick brown fox jumps over the lazy dog", "own", " ", 10, 0.3333)]
        [TestCase("The quick brown fox jumps over the lazy dog", "brow", " ", 10, 0.75)]
        [TestCase("The quick brown fox jumps over the lazy dog", "og", " ", 40, 0.5)]
        [TestCase("The quick brown fox jumps over the lazy dog", "do", " ", 40, 0.5)]
        public void should_find_approx_matches_that_are_not_words_with_lower_score(string text, string pattern, string delimiters, int expected, double score)
        {
            var match = text.FuzzyMatch(pattern, 0, new HashSet<char>(delimiters));
            match.Item1.Should().Be(expected);
            match.Item3.Should().BeApproximately(score, 0.001);
        }

        [TestCase("The quick brown fox jumps over the lazy dog", "ovr", " ", 26, 4, 0.6667)]
        [TestCase("The quick brown fox jumps over the lazy dog", "eover", " ", 26, 4, 0.8)]
        [TestCase("The quick brown fox jumps over the lazy dog", "jmps over", " ", 20, 10, 0.8888)]
        [TestCase("The quick brown fox jumps over the lazy dog", "jmps ovr", " ", 20, 10, 0.75)]
        [TestCase("The quick brown fox jumps over the lazy dog", "jumpss oveor", " ", 20, 10, 0.8334)]
        [TestCase("The quick brown fox jumps over the lazy dog", "jummps ovver", " ", 20, 10, 0.8334)]
        [TestCase("The quick brown fox jumps over the lazy dog", "hhumps over", " ", 20, 10, 0.8182)]
        [TestCase("The quick brown fox jumps over the lazy dog", "hhumps ov", " ", 20, 10, 0.5556)]
        [TestCase("The quick brown fox jumps over the lazy dog", "jumps ovea", " ", 20, 10, 0.9)]
        [TestCase("The Hero George R R Martin", "George R.R. Martin", " .,_-=()[]|\"`'’", 9, 17, 0.8888)]
        public void should_match_on_word_boundaries(string text, string pattern, string delimiters, int location, int length, double score)
        {
            var match = text.FuzzyMatch(pattern, wordDelimiters: new HashSet<char>(delimiters));

            match.Item1.Should().Be(location);
            match.Item2.Should().Be(length);
            match.Item3.Should().BeApproximately(score, 0.001);
        }
    }
}
