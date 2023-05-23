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
            var one = calculator.One;
            var allOnes = calculator.BitwiseComplement(calculator.Zero);
            var one_comp = calculator.BitwiseComplement(one);
            var matchmask = calculator.LeftShift(one, pattern.Length - 1);
            var matchmask_comp = calculator.BitwiseComplement(matchmask);
            var bestLoc = -1;
            var bestLength = 0;

            var lastRd = Array.Empty<T>();
            var r = new List<T[]>(pattern.Length);

            var adjustForWordBoundary = wordDelimiters != null;

            var start = 1;
            var finish = text.Length + pattern.Length;
            var charMatches = new T[finish];

            for (var c = start; c <= finish; c++)
            {
                if (text.Length <= c - 1 || !s.TryGetValue(text[c - 1], out var mask))
                {
                    // Out of range.
                    mask = allOnes;
                }

                charMatches[c - 1] = mask;
            }

            for (var d = 0; d < pattern.Length; d++)
            {
                // Scan for the best match; each iteration allows for one more error.
                var rd = new T[finish + 2];

                rd[finish + 1] = calculator.BitwiseComplement(calculator.Subtract(calculator.LeftShift(one, d), one));

                if (wordDelimiters != null)
                {
                    r.Add(rd);
                }

                for (var j = finish; j >= start; j--)
                {
                    var charMatch = charMatches[j - 1];

                    if (d == 0)
                    {
                        // First pass: exact match.
                        rd[j] = calculator.BitwiseOr(calculator.LeftShift(rd[j + 1], 1), charMatch);

                        if (adjustForWordBoundary)
                        {
                            rd[j] = AdjustForWordBoundary(rd[j], j, text, wordDelimiters, one_comp, allOnes, calculator);
                        }
                    }
                    else
                    {
                        // Subsequent passes: fuzzy match.
                        // state if we assume exact match on char j
                        var rd_match = calculator.BitwiseOr(calculator.LeftShift(rd[j + 1], 1), charMatch);

                        // state if we assume substitution on char j
                        var rd_sub = calculator.LeftShift(lastRd[j + 1], 1);

                        // state if we assume insertion on char j
                        var rd_ins = calculator.LeftShift(lastRd[j], 1);

                        // state if we assume deletion at char j
                        var rd_del = calculator.BitwiseAnd(lastRd[j + 1],  one_comp);

                        if (adjustForWordBoundary)
                        {
                            rd_match = AdjustForWordBoundary(rd_match, j, text, wordDelimiters, one_comp, allOnes, calculator);
                            rd_sub = AdjustForWordBoundary(rd_sub, j, text, wordDelimiters, one_comp, allOnes, calculator);
                            rd_ins = AdjustForWordBoundary(rd_ins, j + 1, text, wordDelimiters, one_comp, allOnes, calculator);
                            rd_del = AdjustForWordBoundary(rd_del, j - 1, text, wordDelimiters, one_comp, allOnes, calculator);
                        }

                        // the final state for this pass
                        rd[j] = calculator.BitwiseAnd(rd_match, rd_sub, rd_ins, rd_del);
                    }

                    if (calculator.NotEqual(calculator.BitwiseOr(rd[j], matchmask_comp), allOnes))
                    {
                        // This match will almost certainly be better than any existing
                        // match.  But check anyway.
                        var score = BitapScore(d, pattern);

                        var isOnWordBoundary = true;

                        if (wordDelimiters != null)
                        {
                            isOnWordBoundary = (j - 1 == 0 || wordDelimiters.Contains(text[j - 2])) && !wordDelimiters.Contains(text[j - 1]);
                        }

                        if (score >= scoreThreshold && isOnWordBoundary)
                        {
                            // Told you so.
                            scoreThreshold = score;
                            bestLoc = j - 1;

                            if (wordDelimiters != null)
                            {
                                var match = GetMatch(j, d, 0, r, matchmask, text, s, calculator);
                                bestLength = match.Count;
                            }
                        }
                    }
                }

                lastRd = rd;

                if (BitapScore(d + 1, pattern) < scoreThreshold)
                {
                    // No hope for a (better) match at greater error levels.
                    break;
                }
            }

            return new Tuple<int, int, double>(bestLoc, bestLength, scoreThreshold);
        }

        private static T AdjustForWordBoundary<T>(T rdj, int j, string text, HashSet<char> delimiters, T one_comp, T allOnes, Calculator<T> calculator)
        {
            // if rdj == 1 then we are starting a new match. Only allow if on a word boundary
            if (calculator.Equal(rdj, one_comp) && j < text.Length && !delimiters.Contains(text[j]))
            {
                return allOnes;
            }

            return rdj;
        }

        private static List<char> GetMatch<T>(int j, int d, int shift, List<T[]> r, T matchmask, string text, Dictionary<char, T> s, Calculator<T> calculator)
        {
            if (j > text.Length)
            {
                return new List<char>();
            }

            var curr = text[j - 1];
            var take = true;

            if (!s.TryGetValue(curr, out var charMatch))
            {
                charMatch = calculator.BitwiseComplement(calculator.Zero);
            }

            var rd_match = calculator.LeftShift(calculator.BitwiseComplement(calculator.BitwiseOr(calculator.LeftShift(r[d][j + 1], 1), charMatch)), shift);

            if (calculator.NotEqual(calculator.BitwiseAnd(rd_match, matchmask), calculator.Zero))
            {
                // an exact match on char j
                j++;
                shift++;
            }
            else if (d > 0)
            {
                var rd_ins = calculator.LeftShift(calculator.BitwiseComplement(r[d - 1][j]), shift + 1);
                var rd_sub = calculator.LeftShift(calculator.BitwiseComplement(r[d - 1][j + 1]), shift + 1);
                var rd_del = calculator.LeftShift(calculator.BitwiseComplement(r[d - 1][j + 1]), shift);

                d--;

                if (calculator.NotEqual(calculator.BitwiseAnd(rd_ins, matchmask), calculator.Zero))
                {
                    // actually insertion, don't take the character and run again with same j and bigger shift
                    shift++;
                    take = false;
                }
                else if (calculator.NotEqual(calculator.BitwiseAnd(rd_sub, matchmask), calculator.Zero))
                {
                    //substitution, take and carry on, just like exact
                    shift++;
                    j++;
                }
                else if (calculator.NotEqual(calculator.BitwiseAnd(rd_del, matchmask), calculator.Zero))
                {
                    //actually deletion
                    //don't shift match mask?
                    j++;
                }
            }
            else
            {
                // matchmask is zero or not a match
                return new List<char>();
            }

            var result = GetMatch<T>(j, d, shift, r, matchmask, text, s, calculator);
            if (take)
            {
                result.Insert(0, curr);
            }

            return result;
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

            var i = 0;
            foreach (var c in pattern)
            {
                var mask = calculator.BitwiseComplement(calculator.LeftShift(calculator.One, pattern.Length - i - 1));

                if (s.ContainsKey(c))
                {
                    s[c] = calculator.BitwiseAnd(s[c], mask);
                }
                else
                {
                    s.Add(c, mask);
                }

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
            public abstract T BitwiseAnd(T a, T b, T c, T d);
            public abstract T BitwiseComplement(T a);
            public abstract bool NotEqual(T a, T b);
            public abstract bool Equal(T a, T b);
        }

        private sealed class BigIntCalculator : Calculator<BigInteger>
        {
            public override BigInteger Zero => new BigInteger(0);
            public override BigInteger One => new BigInteger(1);
            public override BigInteger Subtract(BigInteger a, BigInteger b) => a - b;
            public override BigInteger LeftShift(BigInteger a, int shift) => a << shift;
            public override BigInteger BitwiseOr(BigInteger a, BigInteger b) => a | b;
            public override BigInteger BitwiseAnd(BigInteger a, BigInteger b) => a & b;
            public override BigInteger BitwiseAnd(BigInteger a, BigInteger b, BigInteger c, BigInteger d) => a & b & c & d;
            public override BigInteger BitwiseComplement(BigInteger a) => ~a;
            public override bool NotEqual(BigInteger a, BigInteger b) => a != b;
            public override bool Equal(BigInteger a, BigInteger b) => a == b;
        }

        private sealed class IntCalculator : Calculator<int>
        {
            public override int Zero => 0;
            public override int One => 1;
            public override int Subtract(int a, int b) => a - b;
            public override int LeftShift(int a, int shift) => a << shift;
            public override int BitwiseOr(int a, int b) => a | b;
            public override int BitwiseAnd(int a, int b) => a & b;
            public override int BitwiseAnd(int a, int b, int c, int d) => a & b & c & d;
            public override int BitwiseComplement(int a) => ~a;
            public override bool NotEqual(int a, int b) => a != b;
            public override bool Equal(int a, int b) => a == b;
        }

        private sealed class LongCalculator : Calculator<long>
        {
            public override long Zero => 0;
            public override long One => 1;
            public override long Subtract(long a, long b) => a - b;
            public override long LeftShift(long a, int shift) => a << shift;
            public override long BitwiseOr(long a, long b) => a | b;
            public override long BitwiseAnd(long a, long b) => a & b;
            public override long BitwiseAnd(long a, long b, long c, long d) => a & b & c & d;
            public override long BitwiseComplement(long a) => ~a;
            public override bool NotEqual(long a, long b) => a != b;
            public override bool Equal(long a, long b) => a == b;
        }
    }
}
