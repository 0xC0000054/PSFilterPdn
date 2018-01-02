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

using System;
using System.Collections.ObjectModel;

namespace PSFilterLoad.PSApi
{
    internal interface IActionListSuite
    {
        /// <summary>
        /// Gets the values associated with the specified list.
        /// </summary>
        /// <param name="list">The list.</param>
        /// <param name="values">The values contained within the list.</param>
        /// <returns><c>true</c> if the list is valid; otherwise, <c>false</c>.</returns>
        bool TryGetListValues(IntPtr list, out ReadOnlyCollection<ActionListItem> values);

        /// <summary>
        /// Creates a list that contains the specified values.
        /// </summary>
        /// <param name="values">The values to place in the list.</param>
        /// <returns>The new list.</returns>
        IntPtr CreateList(ReadOnlyCollection<ActionListItem> values);
    }
}
