/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2017 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace PSFilterPdn
{
    internal static class StringExtensions
    {
        internal static bool Contains(this string s, string value, StringComparison comparisonType)
        {
            return (s.IndexOf(value, comparisonType) >= 0);
        }
    }
}
