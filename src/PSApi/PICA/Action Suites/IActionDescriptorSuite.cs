﻿/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2024 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace PSFilterLoad.PSApi.PICA
{
    internal interface IActionDescriptorSuite
    {
        /// <summary>
        /// Gets the values associated with the specified descriptor.
        /// </summary>
        /// <param name="descriptor">The descriptor.</param>
        /// <param name="values">The values contained within the descriptor.</param>
        /// <returns><c>true</c> if the descriptor is valid; otherwise, <c>false</c>.</returns>
        bool TryGetDescriptorValues(PIActionDescriptor descriptor, [MaybeNullWhen(false)] out Dictionary<uint, AETEValue> values);

        /// <summary>
        /// Creates a descriptor that contains the specified values.
        /// </summary>
        /// <param name="values">The values to place in the descriptor.</param>
        /// <returns>The new descriptor.</returns>
        PIActionDescriptor CreateDescriptor(Dictionary<uint, AETEValue> values);
    }
}
