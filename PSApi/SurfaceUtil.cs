/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2019 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PaintDotNet;
using System;

namespace PSFilterLoad.PSApi
{
    internal static class SurfaceUtil
    {
        /// <summary>
        /// Determines whether the specified surface image has transparent pixels.
        /// </summary>
        /// <param name="surface">The surface to check.</param>
        /// <returns>
        ///   <c>true</c> if the surface has transparent pixels; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="surface"/> is null.</exception>
        internal static unsafe bool HasTransparentPixels(Surface surface)
        {
            if (surface == null)
            {
                throw new ArgumentNullException(nameof(surface));
            }

            for (int y = 0; y < surface.Height; y++)
            {
                ColorBgra* src = surface.GetRowAddressUnchecked(y);
                ColorBgra* srcEnd = src + surface.Width;

                while (src < srcEnd)
                {
                    if (src->A < 255)
                    {
                        return true;
                    }

                    src++;
                }
            }

            return false;
        }
    }
}
