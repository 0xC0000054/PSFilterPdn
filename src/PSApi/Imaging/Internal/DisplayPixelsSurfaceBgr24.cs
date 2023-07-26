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

// Adapted from:
/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Drawing;

#nullable enable

namespace PSFilterLoad.PSApi.Imaging.Internal
{
    internal sealed unsafe class DisplayPixelsSurfaceBgr24 : DisplayPixelsSurface
    {
        private readonly MemoryBlock scan0;
        private readonly int stride;

        public DisplayPixelsSurfaceBgr24(int width, int height)
        {
            Width = width;
            Height = height;

            // GDI+ requires the stride to be padded to a multiple of 4 bytes.
            stride = checked(((width * 3) + 3) & ~3);
            scan0 = new MemoryBlock((long)stride * height);
        }

        public override int Width { get; }

        public override int Height { get; }

        public override int ChannelCount => 3;

        public override SurfacePixelFormat Format => SurfacePixelFormat.Bgr24;

        public override bool SupportsTransparency => false;

        public override IDisplayPixelsSurfaceLock Lock(SurfaceLockMode mode)
            => new DisplayPixelsSurfaceBgr24Lock(scan0.VoidStar, stride, Width, Height);

        public override IDisplayPixelsSurfaceLock Lock(Rectangle bounds, SurfaceLockMode mode)
        {
            VerifyNotDisposed();

            Rectangle original = new(0, 0, Width, Height);
            Rectangle sub = bounds;
            Rectangle clipped = Rectangle.Intersect(original, sub);

            if (clipped != sub)
            {
                throw new ArgumentOutOfRangeException(nameof(bounds), bounds,
                    "bounds parameters must be a subset of this Surface's bounds");
            }

            byte* buffer = (byte*)scan0.VoidStar + (((long)stride * bounds.Y) + ((long)bounds.X * 3));

            return new DisplayPixelsSurfaceBgr24Lock(buffer, stride, Width, Height);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                scan0.Dispose();
            }
        }
    }
}
