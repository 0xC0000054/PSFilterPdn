/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2023 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterLoad.PSApi.Imaging.Internal;

#nullable enable

namespace PSFilterLoad.PSApi.Imaging
{
    internal sealed class DisplayPixelsSurfaceFactory : IDisplayPixelsSurfaceFactory
    {
        public static readonly IDisplayPixelsSurfaceFactory Instance = new DisplayPixelsSurfaceFactory();

        public DisplayPixelsSurface Create(int width, int height, bool hasTransparency)
        {
            DisplayPixelsSurface surface;

            if (hasTransparency)
            {
                surface = new DisplayPixelsSurfaceBgra32(width, height);
            }
            else
            {
                surface = new DisplayPixelsSurfaceBgr24(width, height);
            }

            return surface;
        }
    }
}
