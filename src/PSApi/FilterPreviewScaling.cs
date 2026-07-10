/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2026 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace PSFilterLoad.PSApi
{
    internal static class FilterPreviewScaling
    {
        /// <summary>
        /// Gets the scale factor for the preview image as an integer.
        /// </summary>
        /// <param name="rate">The scaling rate.</param>
        /// <returns>
        /// The integer scaling factor.
        /// </returns>
        /// <remarks>
        /// The scaling factor is used as a divisor to calculate the
        /// requested size of the preview image.
        /// Factors greater than one will shrink the image.
        /// </remarks>
        internal static int GetIntegerScaleFactor(Fixed16 rate)
        {
            int scaleFactor = 1;

            // Photoshop 2.5 filters can have a scaling factor of zero,
            // which is treated as a scaling factor of one.
            if (rate != Fixed16.One && rate != Fixed16.Zero)
            {
                // From the Photoshop SDK documentation, hosts that only support integer scaling
                // factors should round floating point values to the nearest integer.
                // As an optimization, we check if the value is an integer before gong down the
                // floating point code path.
                scaleFactor = rate.IsInteger ? rate.ToInt32() : (int)Math.Round(rate.ToDouble());
            }

            return scaleFactor;
        }
    }
}
