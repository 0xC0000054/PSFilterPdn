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

using System.Drawing;

#nullable enable

namespace PSFilterLoad.PSApi.Imaging
{
    internal interface IDisplayPixelsSurface : ISurface<DisplayPixelsSurface>
    {
        public new IDisplayPixelsSurfaceLock Lock(SurfaceLockMode mode);

        public new IDisplayPixelsSurfaceLock Lock(Rectangle bounds, SurfaceLockMode mode);
    }
}
