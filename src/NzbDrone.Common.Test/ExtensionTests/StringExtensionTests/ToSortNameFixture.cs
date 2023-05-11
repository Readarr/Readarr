using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Common.Test.ExtensionTests.StringExtensionTests
{
    [TestFixture]
    public class ToSortNameFixture
    {
        [TestCase("a[b]c(d)e{f}g<h>i", "aceg<h>i")]
        [TestCase("a[[b]c(d)e{f}]g(h(i)j[k]l{m})n{{{o}}}p", "agnp")]
        [TestCase("a[b(c]d)e", "ae")]
        [TestCase("a{b(c}d)e", "ae")]
        [TestCase("a]b}c)d", "abcd")]
        [TestCase("a[b]c]d(e)f{g)h}i}j)k]l", "acdfijkl")]
        [TestCase("a]b[c", "ab")]
        [TestCase("a(b[c]d{e}f", "a")]
        [TestCase("a{b}c{d[e]f(g)h", "ac")]
        public void should_remove_brackets(string input, string expected)
        {
            input.RemoveBracketedText().Should().Be(expected);
        }

        [TestCase("Aristotle", "Aristotle")]
        [TestCase("Mr. Dr Prof.", "Mr. Dr Prof.")]
        [TestCase("Senior Inc", "Senior Inc")]
        [TestCase("Don \"Team\" Smith", "Smith, Don \"Team\"")]
        [TestCase("Don Team Smith", "Don Team Smith")]
        [TestCase("National Lampoon", "National Lampoon")]
        [TestCase("Jane Doe", "Doe, Jane")]
        [TestCase("Mrs. Jane Q. Doe III", "Doe, Jane Q. III")]
        [TestCase("Leonardo Da Vinci", "Da Vinci, Leonardo")]
        [TestCase("Van Gogh", "Van Gogh")]
        [TestCase("Van", "Van")]
        [TestCase("John [x]von Neumann (III)", "von Neumann, John")]
        public void should_get_sort_name(string input, string expected)
        {
            input.ToLastFirst().Should().Be(expected);
        }
    }
}
