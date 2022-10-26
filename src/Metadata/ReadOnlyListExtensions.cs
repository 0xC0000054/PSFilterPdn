/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2022 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PaintDotNet.Collections;
using System;
using System.Collections.Generic;

namespace PSFilterPdn.Metadata
{
    internal static class ReadOnlyListExtensions
    {
        internal static T[] AsArrayOrToArray<T>(this IReadOnlyList<T> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            return items is T[] asArray ? asArray : EnumerableExtensions.ToArrayEx(items);
        }
    }
}
