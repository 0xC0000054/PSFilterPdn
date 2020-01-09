/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2020 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.ObjectModel;

namespace PSFilterLoad.PSApi.PICA
{
    internal interface IActionReferenceSuite
    {
        /// <summary>
        /// Gets the values associated with the specified reference.
        /// </summary>
        /// <param name="list">The reference.</param>
        /// <param name="values">The values contained within the list.</param>
        /// <returns><c>true</c> if the reference is valid; otherwise, <c>false</c>.</returns>
        bool TryGetReferenceValues(PIActionReference reference, out ReadOnlyCollection<ActionReferenceItem> values);

        /// <summary>
        /// Creates a reference that contains the specified values.
        /// </summary>
        /// <param name="values">The values to place in the reference.</param>
        /// <returns>The new reference.</returns>
        PIActionReference CreateReference(ReadOnlyCollection<ActionReferenceItem> values);
    }
}
