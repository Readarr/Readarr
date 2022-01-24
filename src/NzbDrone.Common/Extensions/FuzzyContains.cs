    /*
 * This file incorporates work covered by the following copyright and
 * permission notice:
 *
 * Diff Match and Patch
 * Copyright 2018 The diff-match-patch Authors.
 * https://github.com/google/diff-match-patch
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace NzbDrone.Common.Extensions
{
    public static class FuzzyContainsExtension
    {
        public static int FuzzyFind(this string text, string pattern, double matchProb)
        {
            return FuzzyMatch(text, pattern, matchProb).Item1;
        }

        // return the accuracy of the best match of pattern within text
        public static double FuzzyContains(this string text, string pattern)
        {
            return FuzzyMatch(text, pattern, 0.25).Item3;
        }

        /**
         * Locate the best instance of 'pattern' in 'text'.
         * Returns (-1, 1) if no match found.
         * @param text The text to search.
         * @param pattern The pattern to search for.
         * @return Best match index or -1.
         */
        public static Tuple<int, int, double> FuzzyMatch(this string text, string pattern, double matchThreshold = 0.5, HashSet<char> wordDelimiters = null)
        {
            // Check for null inputs not needed since null can't be passed in C#.
            if (text.Length == 0 || pattern.Length == 0)
            {
                // Nothing to match.
                return new Tuple<int, int, double>(-1, 0, 0);
            }

            if (pattern.Length <= text.Length && wordDelimiters == null)
            {
                var loc = text.IndexOf(pattern, StringComparison.Ordinal);
                if (loc != -1)
                {
                    // Perfect match!
                    return new Tuple<int, int, double>(loc, pattern.Length, 1);
                }
            }

            // Do a fuzzy compare.
            if (pattern.Length < 32)
            {
                return MatchBitap(text, pattern, matchThreshold, new IntCalculator(), wordDelimiters);
            }

            if (pattern.Length < 64)
            {
                return MatchBitap(text, pattern, matchThreshold, new LongCalculator(), wordDelimiters);
            }

            return MatchBitap(text, pattern, matchThreshold, new BigIntCalculator(), wordDelimiters);
        }

        /**
         * Locate the best instance of 'pattern' in 'text' near 'loc' using the
         * Bitap algorithm.  Returns -1 if no match found.
         * @param text The text to search.
         * @param pattern The pattern to search for.
         * @return Best match index or -1.
         */
        private static Tuple<int, int, double> MatchBitap<T>(string text, string pattern, double matchThreshold, Calculator<T> calculator, HashSet<char> wordDelimiters = null)
        {
            // Initialise the alphabet.
            var s = Alphabet(pattern, calculator);

            // Lowest score below which we give up.
            var scoreThreshold = matchThreshold;

            // Initialise the bit arrays.
            var matchmask = calculator.LeftShift(calculator.One, pattern.Length - 1);
            var bestLoc = -1;
            var bestLength = 0;

            var lastRd = Array.Empty<T>();
            var lastMd = Array.Empty<List<int>>();

            for (var d = 0; d < pattern.Length; d++)
            {
                // Scan for the best match; each iteration allows for one more error.
                var start = 1;
                var finish = text.Length + pattern.Length;

                var rd = new T[finish + 2];
                rd[finish + 1] = calculator.Subtract(calculator.LeftShift(calculator.One, d), calculator.One);

                var md = new List<int>[finish + 2];
                md[finish + 1] = new List<int>();

                for (var j = finish; j >= start; j--)
                {
                    T charMatch;
                    T rd_exact, rd_last, rd_curr, rd_a, rd_b;
                    List<int> md_exact, md_last, md_curr, md_a, md_b;

                    if (text.Length <= j - 1 || !s.TryGetValue(text[j - 1], out charMatch))
                    {
                        // Out of range.
                        charMatch = calculator.Zero;
                    }

                    if (d == 0)
                    {
                        // First pass: exact match.
                        rd[j] = calculator.BitwiseAnd(calculator.BitwiseOr(calculator.LeftShift(rd[j + 1], 1), calculator.One), charMatch);

                        if (wordDelimiters != null)
                        {
                            if (calculator.NotEqual(rd[j], calculator.Zero))
                            {
                                md[j] = md[j + 1].Any() ? md[j + 1].SelectList(x => x + 1) : new List<int> { 1 };
                            }
                            else
                            {
                                md[j] = new List<int>();
                            }
                        }
                    }
                    else
                    {
                        // Subsequent passes: fuzzy match.
                        // state if we assume exact match on char j
                        rd_exact = calculator.BitwiseAnd(calculator.BitwiseOr(calculator.LeftShift(rd[j + 1], 1), calculator.One), charMatch);

                        // state if we assume substitution on char j
                        rd_a = calculator.LeftShift(lastRd[j + 1], 1);

                        // state if we assume deletion on char j
                        rd_b = calculator.LeftShift(lastRd[j], 1);

                        // state if we assume insertion at char j
                        rd_last = lastRd[j + 1];

                        // the final state for this pass
                        rd_curr = calculator.BitwiseOr(rd_exact,
                            calculator.BitwiseOr(rd_a,
                                calculator.BitwiseOr(rd_b,
                                    calculator.BitwiseOr(calculator.One,
                                        rd_last))));

                        rd[j] = rd_curr;

                        if (wordDelimiters != null)
                        {
                            // exact match
                            if (calculator.NotEqual(rd_exact, calculator.Zero))
                            {
                                md_exact = md[j + 1].Any() ? md[j + 1].SelectList(x => x + 1) : new List<int> { 1 };
                            }
                            else
                            {
                                md_exact = new List<int>();
                            }

                            // substitution
                            md_a = lastMd[j + 1].Any() ? lastMd[j + 1].SelectList(x => x + 1) : new List<int> { 1 };

                            // deletion
                            md_b = lastMd[j].Any() ? lastMd[j] : new List<int> { 1 };

                            // insertion
                            md_last = lastMd[j].Any() ? lastMd[j + 1].SelectList(x => x + 1) : new List<int> { 1 };

                            // combined
                            md_curr = md_exact.Concat(md_a).Concat(md_b).Concat(md_last).Distinct().ToList();

                            md[j] = md_curr;
                        }
                    }

                    if (calculator.NotEqual(calculator.BitwiseAnd(rd[j], matchmask), calculator.Zero))
                    {
                        // This match will almost certainly be better than any existing
                        // match.  But check anyway.
                        var score = BitapScore(d, pattern);

                        bool isOnWordBoundary;
                        var endsOnWordBoundaryLength = 0;

                        if (wordDelimiters != null)
                        {
                            var startsOnWordBoundary = (j - 1 == 0 || wordDelimiters.Contains(text[j - 2])) && !wordDelimiters.Contains(text[j - 1]);
                            endsOnWordBoundaryLength = md[j].FirstOrDefault(x => (j + x >= text.Length || wordDelimiters.Contains(text[j - 1 + x])) && !wordDelimiters.Contains(text[j - 1]));
                            isOnWordBoundary = startsOnWordBoundary && endsOnWordBoundaryLength > 0;
                        }
                        else
                        {
                            isOnWordBoundary = true;
                        }

                        if (score >= scoreThreshold && isOnWordBoundary)
                        {
                            // Told you so.
                            scoreThreshold = score;
                            bestLoc = j - 1;
                            bestLength = endsOnWordBoundaryLength;
                        }
                    }
                }

                if (BitapScore(d + 1, pattern) < scoreThreshold)
                {
                    // No hope for a (better) match at greater error levels.
                    break;
                }

                lastRd = rd;
                lastMd = md;
            }

            return new Tuple<int, int, double>(bestLoc, bestLength, scoreThreshold);
        }

        /**
         * Compute and return the score for a match with e errors and x location.
         * @param e Number of errors in match.
         * @param pattern Pattern being sought.
         * @return Overall score for match (1.0 = good, 0.0 = bad).
         */
        private static double BitapScore(int e, string pattern)
        {
            return 1.0 - ((double)e / pattern.Length);
        }

        /**
         * Initialise the alphabet for the Bitap algorithm.
         * @param pattern The text to encode.
         * @return Hash of character locations.
         */
        private static Dictionary<char, T> Alphabet<T>(string pattern, Calculator<T> calculator)
        {
            var s = new Dictionary<char, T>();
            var charPattern = pattern.ToCharArray();
            foreach (var c in charPattern)
            {
                if (!s.ContainsKey(c))
                {
                    s.Add(c, calculator.Zero);
                }
            }

            var i = 0;
            foreach (var c in charPattern)
            {
                s[c] = calculator.BitwiseOr(s[c], calculator.LeftShift(calculator.One, pattern.Length - i - 1));
                i++;
            }

            return s;
        }

        private abstract class Calculator<T>
        {
            public abstract T Zero { get; }
            public abstract T One { get; }
            public abstract T Subtract(T a, T b);
            public abstract T LeftShift(T a, int shift);
            public abstract T BitwiseOr(T a, T b);
            public abstract T BitwiseAnd(T a, T b);
            public abstract bool NotEqual(T a, T b);
        }

        private sealed class BigIntCalculator : Calculator<BigInteger>
        {
            public override BigInteger Zero => new BigInteger(0);
            public override BigInteger One => new BigInteger(1);
            public override BigInteger Subtract(BigInteger a, BigInteger b) => a - b;
            public override BigInteger LeftShift(BigInteger a, int shift) => a << shift;
            public override BigInteger BitwiseOr(BigInteger a, BigInteger b) => a | b;
            public override BigInteger BitwiseAnd(BigInteger a, BigInteger b) => a & b;
            public override bool NotEqual(BigInteger a, BigInteger b) => a != b;
        }

        private sealed class IntCalculator : Calculator<int>
        {
            public override int Zero => 0;
            public override int One => 1;
            public override int Subtract(int a, int b) => a - b;
            public override int LeftShift(int a, int shift) => a << shift;
            public override int BitwiseOr(int a, int b) => a | b;
            public override int BitwiseAnd(int a, int b) => a & b;
            public override bool NotEqual(int a, int b) => a != b;
        }

        private sealed class LongCalculator : Calculator<long>
        {
            public override long Zero => 0;
            public override long One => 1;
            public override long Subtract(long a, long b) => a - b;
            public override long LeftShift(long a, int shift) => a << shift;
            public override long BitwiseOr(long a, long b) => a | b;
            public override long BitwiseAnd(long a, long b) => a & b;
            public override bool NotEqual(long a, long b) => a != b;
        }
    }
}
