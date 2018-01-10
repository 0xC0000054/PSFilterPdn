/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2018 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;

namespace PSFilterPdn
{
    internal static class ListExtensions
    {
        /// <summary>
        /// Determines whether the specified lists contain the same elements, ignoring order and duplicates.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the collection.</typeparam>
        /// <param name="first">The first list.</param>
        /// <param name="second">The second list.</param>
        /// <param name="comparer">The comparer.</param>
        /// <returns>
        ///   <c>true</c> if the lists contain the same elements; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="first"/> is null.
        /// or
        /// <paramref name="second"/> is null.
        /// </exception>
        public static bool SetEqual<T>(this IList<T> first, IList<T> second, IEqualityComparer<T> comparer)
        {
            return new HashSet<T>(first, comparer).SetEquals(second);
        }
    }
}
