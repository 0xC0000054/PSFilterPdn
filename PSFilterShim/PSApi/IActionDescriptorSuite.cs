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

using System;

namespace PSFilterLoad.PSApi
{
    internal interface IActionDescriptorSuite
    {
        /// <summary>
        /// Gets the values associated with the specified descriptor.
        /// </summary>
        /// <param name="descriptor">The descriptor.</param>
        /// <param name="values">The values contained within the descriptor.</param>
        /// <returns><c>true</c> if the descriptor is valid; otherwise, <c>false</c>.</returns>
        bool TryGetDescriptorValues(IntPtr descriptor, out ReadOnlyDictionary<uint, AETEValue> values);

        /// <summary>
        /// Creates a descriptor that contains the specified values.
        /// </summary>
        /// <param name="values">The values to place in the descriptor.</param>
        /// <returns>The new descriptor.</returns>
        IntPtr CreateDescriptor(ReadOnlyDictionary<uint, AETEValue> values);
    }
}
