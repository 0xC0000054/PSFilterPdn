/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2017 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

// Optimized by Richard Deeming
// Original code by Vasian Cepa
// http://www.codeproject.com/Articles/11016/Numeric-String-Sort-in-C

using System.Collections;
using System.Globalization;

namespace PSFilterPdn
{
    internal sealed class StringLogicalComparer : IComparer
    {
        private static readonly IComparer _default = new StringLogicalComparer();
        private static readonly CultureInfo culture = CultureInfo.CurrentCulture;

        private StringLogicalComparer()
        {
        }

        public static IComparer Default
        {
            get { return _default; }
        }

        public int Compare(object x, object y)
        {
            if (null == x && null == y) return 0;
            if (null == x) return -1;
            if (null == y) return 1;

            string left = x as string;
            string right = y as string;

            if (left != null && right != null)
                return Compare(left, right);

            return Comparer.Default.Compare(x, y);
        }

        public static int Compare(string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1))
            {
                if (string.IsNullOrEmpty(s2)) return 0;
                return -1;
            }
            else if (string.IsNullOrEmpty(s2))
            {
                return 1;
            }

            int s1Length = s1.Length;
            int s2Length = s2.Length;

            bool sp1 = char.IsLetterOrDigit(s1[0]);
            bool sp2 = char.IsLetterOrDigit(s2[0]);

            if (sp1 && !sp2) return 1;
            if (!sp1 && sp2) return -1;

            char c1, c2;
            int i1 = 0, i2 = 0;
            int r = 0;
            bool letter1, letter2;

            while (true)
            {
                c1 = s1[i1];
                c2 = s2[i2];

                sp1 = char.IsDigit(c1);
                sp2 = char.IsDigit(c2);

                if (!sp1 && !sp2)
                {
                    if (c1 != c2)
                    {
                        letter1 = char.IsLetter(c1);
                        letter2 = char.IsLetter(c2);

                        if (letter1 && letter2)
                        {
                            c1 = char.ToUpper(c1, culture);
                            c2 = char.ToUpper(c2, culture);

                            r = c1 - c2;
                            if (0 != r) return r;
                        }
                        else if (!letter1 && !letter2)
                        {
                            r = c1 - c2;
                            if (0 != r) return r;
                        }
                        else if (letter1)
                        {
                            return 1;
                        }
                        else if (letter2)
                        {
                            return -1;
                        }
                    }

                }
                else if (sp1 && sp2)
                {
                    r = CompareNumbers(s1, s1Length, ref i1, s2, s2Length, ref i2);
                    if (0 != r) return r;
                }
                else if (sp1)
                {
                    return -1;
                }
                else if (sp2)
                {
                    return 1;
                }

                i1++;
                i2++;

                if (i1 >= s1Length)
                {
                    if (i2 >= s2Length) return 0;
                    return -1;
                }
                else if (i2 >= s2Length)
                {
                    return 1;
                }
            }
        }

        private static int CompareNumbers(
            string s1, int s1Length, ref int i1,
            string s2, int s2Length, ref int i2)
        {
            int nzStart1 = i1, nzStart2 = i2;
            int end1 = i1, end2 = i2;

            ScanNumber(s1, s1Length, i1, ref nzStart1, ref end1);
            ScanNumber(s2, s2Length, i2, ref nzStart2, ref end2);

            int start1 = i1;
            i1 = end1 - 1;
            int start2 = i2;
            i2 = end2 - 1;

            int length1 = end2 - nzStart2;
            int length2 = end1 - nzStart1;

            if (length1 == length2)
            {
                int r;
                for (int j1 = nzStart1, j2 = nzStart2; j1 <= i1; j1++, j2++)
                {
                    r = s1[j1] - s2[j2];
                    if (0 != r) return r;
                }

                length1 = end1 - start1;
                length2 = end2 - start2;

                if (length1 == length2) return 0;
            }

            if (length1 > length2) return -1;
            return 1;
        }

        private static void ScanNumber(string s, int length, int start, ref int nzStart, ref int end)
        {
            nzStart = start;
            end = start;

            bool countZeros = true;
            char c = s[end];

            while (true)
            {
                if (countZeros)
                {
                    if ('0' == c)
                    {
                        nzStart++;
                    }
                    else
                    {
                        countZeros = false;
                    }
                }

                end++;
                if (end >= length) break;

                c = s[end];
                if (!char.IsDigit(c)) break;
            }
        }
    }
}
