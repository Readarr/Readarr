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
            return FuzzyMatch(text, pattern, 0.25).Item2;
        }

        /**
         * Locate the best instance of 'pattern' in 'text'.
         * Returns (-1, 1) if no match found.
         * @param text The text to search.
         * @param pattern The pattern to search for.
         * @return Best match index or -1.
         */
        public static Tuple<int, double> FuzzyMatch(this string text, string pattern, double matchThreshold = 0.5)
        {
            // Check for null inputs not needed since null can't be passed in C#.
            if (text.Length == 0 || pattern.Length == 0)
            {
                // Nothing to match.
                return new Tuple<int, double>(-1, 0);
            }

            if (pattern.Length <= text.Length)
            {
                var loc = text.IndexOf(pattern, StringComparison.Ordinal);
                if (loc != -1)
                {
                    // Perfect match!
                    return new Tuple<int, double>(loc, 1);
                }
            }

            // Do a fuzzy compare.
            if (pattern.Length < 32)
            {
                return MatchBitap(text, pattern, matchThreshold, new IntCalculator());
            }

            if (pattern.Length < 64)
            {
                return MatchBitap(text, pattern, matchThreshold, new LongCalculator());
            }

            return MatchBitap(text, pattern, matchThreshold, new BigIntCalculator());
        }

        /**
         * Locate the best instance of 'pattern' in 'text' near 'loc' using the
         * Bitap algorithm.  Returns -1 if no match found.
         * @param text The text to search.
         * @param pattern The pattern to search for.
         * @return Best match index or -1.
         */
        private static Tuple<int, double> MatchBitap<T>(string text, string pattern, double matchThreshold, Calculator<T> calculator)
        {
            // Initialise the alphabet.
            var s = Alphabet(pattern, calculator);

            // Lowest score below which we give up.
            var scoreThreshold = matchThreshold;

            // Initialise the bit arrays.
            var matchmask = calculator.LeftShift(calculator.One, pattern.Length - 1);
            var bestLoc = -1;

            var lastRd = Array.Empty<T>();
            for (var d = 0; d < pattern.Length; d++)
            {
                // Scan for the best match; each iteration allows for one more error.
                var start = 1;
                var finish = text.Length + pattern.Length;

                var rd = new T[finish + 2];
                rd[finish + 1] = calculator.Subtract(calculator.LeftShift(calculator.One, d), calculator.One);
                for (var j = finish; j >= start; j--)
                {
                    T charMatch;
                    if (text.Length <= j - 1 || !s.ContainsKey(text[j - 1]))
                    {
                        // Out of range.
                        charMatch = calculator.Zero;
                    }
                    else
                    {
                        charMatch = s[text[j - 1]];
                    }

                    if (d == 0)
                    {
                        // First pass: exact match.
                        rd[j] = calculator.BitwiseAnd(calculator.BitwiseOr(calculator.LeftShift(rd[j + 1], 1), calculator.One), charMatch);
                    }
                    else
                    {
                        // Subsequent passes: fuzzy match.
                        rd[j] = calculator.BitwiseOr(calculator.BitwiseAnd(calculator.BitwiseOr(calculator.LeftShift(rd[j + 1], 1), calculator.One), charMatch),
                            calculator.BitwiseOr(calculator.BitwiseOr(calculator.LeftShift(calculator.BitwiseOr(lastRd[j + 1], lastRd[j]), 1), calculator.One), lastRd[j + 1]));
                    }

                    if (calculator.NotEqual(calculator.BitwiseAnd(rd[j], matchmask), calculator.Zero))
                    {
                        var score = BitapScore(d, pattern);

                        // This match will almost certainly be better than any existing
                        // match.  But check anyway.
                        if (score >= scoreThreshold)
                        {
                            // Told you so.
                            scoreThreshold = score;
                            bestLoc = j - 1;
                        }
                    }
                }

                if (BitapScore(d + 1, pattern) < scoreThreshold)
                {
                    // No hope for a (better) match at greater error levels.
                    break;
                }

                lastRd = rd;
            }

            return new Tuple<int, double>(bestLoc, scoreThreshold);
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
