using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace NzbDrone.Common.Extensions
{
    public static class StringExtensions
    {
        private static readonly Regex CamelCaseRegex = new Regex("(?<!^)[A-Z]", RegexOptions.Compiled);

        public static string NullSafe(this string target)
        {
            return ((object)target).NullSafe().ToString();
        }

        public static object NullSafe(this object target)
        {
            if (target != null)
            {
                return target;
            }

            return "[NULL]";
        }

        public static string FirstCharToLower(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            return char.ToLowerInvariant(input.First()) + input.Substring(1);
        }

        public static string FirstCharToUpper(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            return char.ToUpperInvariant(input.First()) + input.Substring(1);
        }

        public static string Inject(this string format, params object[] formattingArgs)
        {
            return string.Format(format, formattingArgs);
        }

        private static readonly Regex CollapseSpace = new Regex(@"\s+", RegexOptions.Compiled);

        public static string Replace(this string text, int index, int length, string replacement)
        {
            text = text.Remove(index, length);
            text = text.Insert(index, replacement);
            return text;
        }

        public static string RemoveAccent(this string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        public static string TrimEnd(this string text, string postfix)
        {
            if (text.EndsWith(postfix))
            {
                text = text.Substring(0, text.Length - postfix.Length);
            }

            return text;
        }

        public static string Join(this IEnumerable<string> values, string separator)
        {
            return string.Join(separator, values);
        }

        public static string CleanSpaces(this string text)
        {
            return CollapseSpace.Replace(text, " ").Trim();
        }

        public static bool IsNullOrWhiteSpace(this string text)
        {
            return string.IsNullOrWhiteSpace(text);
        }

        public static bool IsNotNullOrWhiteSpace(this string text)
        {
            return !string.IsNullOrWhiteSpace(text);
        }

        public static bool StartsWithIgnoreCase(this string text, string startsWith)
        {
            return text.StartsWith(startsWith, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool EqualsIgnoreCase(this string text, string equals)
        {
            return text.Equals(equals, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool ContainsIgnoreCase(this string text, string contains)
        {
            return text.IndexOf(contains, StringComparison.InvariantCultureIgnoreCase) > -1;
        }

        public static string WrapInQuotes(this string text)
        {
            if (!text.Contains(" "))
            {
                return text;
            }

            return "\"" + text + "\"";
        }

        public static byte[] HexToByteArray(this string input)
        {
            return Enumerable.Range(0, input.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(input.Substring(x, 2), 16))
                             .ToArray();
        }

        public static string ToHexString(this byte[] input)
        {
            return string.Concat(Array.ConvertAll(input, x => x.ToString("X2")));
        }

        public static string FromOctalString(this string octalValue)
        {
            octalValue = octalValue.TrimStart('\\');

            var first = int.Parse(octalValue.Substring(0, 1));
            var second = int.Parse(octalValue.Substring(1, 1));
            var third = int.Parse(octalValue.Substring(2, 1));
            var byteResult = (byte)((first << 6) | (second << 3) | third);

            return Encoding.ASCII.GetString(new[] { byteResult });
        }

        public static string SplitCamelCase(this string input)
        {
            return CamelCaseRegex.Replace(input, match => " " + match.Value);
        }

        public static double FuzzyMatch(this string a, string b)
        {
            if (a.IsNullOrWhiteSpace() || b.IsNullOrWhiteSpace())
            {
                return 0;
            }
            else if (a.Contains(" ") && b.Contains(" "))
            {
                var partsA = a.Split(' ');
                var partsB = b.Split(' ');

                var coef = (FuzzyMatchComponents(partsA, partsB) + FuzzyMatchComponents(partsB, partsA)) / (partsA.Length + partsB.Length);
                return Math.Max(coef, LevenshteinCoefficient(a, b));
            }
            else
            {
                return LevenshteinCoefficient(a, b);
            }
        }

        private static double FuzzyMatchComponents(string[] a, string[] b)
        {
            double weightDenom = Math.Max(a.Length, b.Length);
            double sum = 0;
            for (var i = 0; i < a.Length; i++)
            {
                var high = 0.0;
                var indexDistance = 0;
                for (var x = 0; x < b.Length; x++)
                {
                    var coef = LevenshteinCoefficient(a[i], b[x]);
                    if (coef > high)
                    {
                        high = coef;
                        indexDistance = Math.Abs(i - x);
                    }
                }

                sum += (1.0 - (indexDistance / weightDenom)) * high;
            }

            return sum;
        }

        public static double LevenshteinCoefficient(this string a, string b)
        {
            return 1.0 - ((double)a.LevenshteinDistance(b) / Math.Max(a.Length, b.Length));
        }

        private static readonly HashSet<string> Copywords = new HashSet<string>
        {
            "agency", "corporation", "company", "co.", "council",
            "committee", "inc.", "institute", "national",
            "society", "club", "team"
        };

        private static readonly HashSet<string> SurnamePrefixes = new HashSet<string>
        {
            "da", "de", "di", "la", "le", "van", "von"
        };

        private static readonly HashSet<string> Prefixes = new HashSet<string>
        {
            "mr", "mr.", "mrs", "mrs.", "ms", "ms.", "dr", "dr.", "prof", "prof."
        };

        private static readonly HashSet<string> Suffixes = new HashSet<string>
        {
            "jr", "sr", "inc", "ph.d", "phd",
            "md", "m.d", "i", "ii", "iii", "iv",
            "junior", "senior"
        };

        private static readonly Dictionary<char, char> Brackets = new Dictionary<char, char>
        {
            { '(', ')' },
            { '[', ']' },
            { '{', '}' }
        };

        private static readonly Dictionary<char, char> RMap = Brackets.ToDictionary(x => x.Value, x => x.Key);

        public static string RemoveBracketedText(this string input)
        {
            var counts = Brackets.ToDictionary(x => x.Key, y => 0);
            var total = 0;
            var buf = new List<char>(input.Length);

            foreach (var c in input)
            {
                if (Brackets.ContainsKey(c))
                {
                    counts[c] += 1;
                    total += 1;
                }
                else if (RMap.ContainsKey(c))
                {
                    var idx = RMap[c];
                    if (counts[idx] > 0)
                    {
                        counts[idx] -= 1;
                        total -= 1;
                    }
                }
                else if (total < 1)
                {
                    buf.Add(c);
                }
            }

            return new string(buf.ToArray());
        }

        public static string ToLastFirst(this string author)
        {
            // ported from https://github.com/kovidgoyal/calibre/blob/master/src/calibre/ebooks/metadata/__init__.py
            if (author == null)
            {
                return null;
            }

            var sauthor = author.RemoveBracketedText().Trim();

            var tokens = sauthor.Split();

            if (tokens.Length < 2)
            {
                return author;
            }

            var ltoks = tokens.Select(x => x.ToLowerInvariant()).ToHashSet();

            if (ltoks.Intersect(Copywords).Any())
            {
                return author;
            }

            if (tokens.Length == 2 && SurnamePrefixes.Contains(tokens[0].ToLowerInvariant()))
            {
                return author;
            }

            int first;
            for (first = 0; first < tokens.Length; first++)
            {
                if (!Prefixes.Contains(tokens[first].ToLowerInvariant()))
                {
                    break;
                }
            }

            if (first == tokens.Length)
            {
                return author;
            }

            int last;
            for (last = tokens.Length - 1; last >= first; last--)
            {
                if (!Suffixes.Contains(tokens[last].ToLowerInvariant()))
                {
                    break;
                }
            }

            if (last < first)
            {
                return author;
            }

            var suffix = tokens.TakeLast(tokens.Length - last - 1).ConcatToString(" ");

            if (last > first && SurnamePrefixes.Contains(tokens[last - 1].ToLowerInvariant()))
            {
                tokens[last - 1] += ' ' + tokens[last];
                last -= 1;
            }

            var atokens = new[] { tokens[last] }.Concat(tokens.Skip(first).Take(last - first)).ToList();
            var addComma = atokens.Count > 1;

            if (suffix.IsNotNullOrWhiteSpace())
            {
                atokens.Add(suffix);
            }

            if (addComma)
            {
                atokens[0] += ',';
            }

            return atokens.ConcatToString(" ");
        }

        public static string EncodeRFC3986(this string value)
        {
            // From Twitterizer http://www.twitterizer.net/
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var encoded = Uri.EscapeDataString(value);

            return Regex
                .Replace(encoded, "(%[0-9a-f][0-9a-f])", c => c.Value.ToUpper())
                .Replace("(", "%28")
                .Replace(")", "%29")
                .Replace("$", "%24")
                .Replace("!", "%21")
                .Replace("*", "%2A")
                .Replace("'", "%27")
                .Replace("%7E", "~");
        }

        public static bool IsValidIpAddress(this string value)
        {
            if (!IPAddress.TryParse(value, out var parsedAddress))
            {
                return false;
            }

            if (parsedAddress.Equals(IPAddress.Parse("255.255.255.255")))
            {
                return false;
            }

            if (parsedAddress.IsIPv6Multicast)
            {
                return false;
            }

            return parsedAddress.AddressFamily == AddressFamily.InterNetwork || parsedAddress.AddressFamily == AddressFamily.InterNetworkV6;
        }

        public static string ToUrlHost(this string input)
        {
            return input.Contains(":") ? $"[{input}]" : input;
        }
    }
}
