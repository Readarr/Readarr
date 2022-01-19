using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Extensions;
using NzbDrone.Test.Common;

namespace NzbDrone.Common.Test
{
    [TestFixture]
    public class LevenshteinDistanceFixture : TestBase
    {
        [TestCase("", "", 0)]
        [TestCase("abc", "abc", 0)]
        [TestCase("abc", "abcd", 1)]
        [TestCase("abcd", "abc", 1)]
        [TestCase("abc", "abd", 1)]
        [TestCase("abc", "adc", 1)]
        [TestCase("abcdefgh", "abcghdef", 4)]
        [TestCase("a.b.c.", "abc", 3)]
        [TestCase("Agents Of SHIELD", "Marvel's Agents Of S.H.I.E.L.D.", 15)]
        [TestCase("Agents of cracked", "Agents of shield", 6)]
        [TestCase("ABCxxx", "ABC1xx", 1)]
        [TestCase("ABC1xx", "ABCxxx", 1)]
        public void LevenshteinDistance(string text, string other, int expected)
        {
            text.LevenshteinDistance(other).Should().Be(expected);
        }

        [TestCase("hello", "hello")]
        [TestCase("hello", "bye")]
        [TestCase("a longer string", "a different long string")]
        public void FuzzyMatchSymmetric(string a, string b)
        {
            a.FuzzyMatch(b).Should().Be(b.FuzzyMatch(a));
        }

        [TestCase("", "", 0)]
        [TestCase("a", "", 0)]
        [TestCase("", "a", 0)]
        public void FuzzyMatchEmptyValuesReturnZero(string a, string b, double expected)
        {
            a.FuzzyMatch(b).Should().Be(expected);
        }

        [TestCase("AVERY", "GARVEY", 3)]
        [TestCase("ADCROFT", "ADDESSI", 5)]
        [TestCase("BAIRD", "BAISDEN", 3)]
        [TestCase("BOGGAN", "BOGGS", 2)]
        [TestCase("CLAYTON", "CLEARY", 5)]
        [TestCase("DYBAS", "DYCKMAN", 4)]
        [TestCase("EMINETH", "EMMERT", 4)]
        [TestCase("GALANTE", "GALICKI", 4)]
        [TestCase("HARDIN", "HARDING", 1)]
        [TestCase("KEHOE", "KEHR", 2)]
        [TestCase("LOWRY", "LUBARSKY", 5)]
        [TestCase("MAGALLAN", "MAGANA", 3)]
        [TestCase("MAYO", "MAYS", 1)]
        [TestCase("MOENY", "MOFFETT", 4)]
        [TestCase("PARE", "PARENT", 2)]
        [TestCase("RAMEY", "RAMFREY", 2)]
        public void BMtest(string a, string b, int expected)
        {
            ModifiedBerghelRoachEditDistance.GetDistance(a, b, 10).Should().Be(expected);
        }
    }
}
