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
using PaintDotNet;

#nullable enable

namespace PSFilterLoad.PSApi.Imaging.Internal
{
    internal sealed unsafe class ShimSurfaceGray8 : ImageSurface
    {
        private readonly int width;
        private readonly int height;
        private readonly int stride;
        private readonly MemoryBlock scan0;

        public ShimSurfaceGray8(int width, int height)
            : this(width, height, zeroFill: true)
        {
        }

        private ShimSurfaceGray8(int width ,int height, bool zeroFill)
        {
            this.width = width;
            this.height = height;
            stride = width;
            scan0 = new MemoryBlock(width * height, zeroFill);
        }

        public override int Width => width;

        public override int Height => height;

        public override int ChannelCount => 1;

        public override int BitsPerChannel => 8;

        public override SurfacePixelFormat Format => SurfacePixelFormat.Gray8;

        public override bool SupportsTransparency => false;

        public override ShimSurfaceGray8 Clone()
        {
            VerifyNotDisposed();

            ShimSurfaceGray8 surface = new(width, height, zeroFill: false);
            surface.CopySurface(this);
            return surface;
        }

        public override ShimSurfaceGray8 CreateScaledSurface(int newWidth, int newHeight)
        {
            VerifyNotDisposed();

            ShimSurfaceGray8 surface = new(newWidth, newHeight);
            surface.FitSurface(this);
            return surface;
        }

        public override ShimSurfaceGray8Lock Lock(SurfaceLockMode mode)
        {
            VerifyNotDisposed();

            return new ShimSurfaceGray8Lock(scan0.VoidStar, stride, width, height);
        }

        public override  ShimSurfaceGray8Lock Lock(Rectangle bounds, SurfaceLockMode mode)
        {
            VerifyNotDisposed();

            Rectangle original = new(0, 0, width, height);
            Rectangle sub = bounds;
            Rectangle clipped = Rectangle.Intersect(original, sub);

            if (clipped != sub)
            {
                throw new ArgumentOutOfRangeException(nameof(bounds), bounds,
                    "bounds parameters must be a subset of this Surface's bounds");
            }

            byte* buffer = GetPointAddressUnchecked(bounds.X, bounds.Y);

            return new ShimSurfaceGray8Lock(buffer, stride, bounds.Width, bounds.Height);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                scan0.Dispose();
            }
        }

        /// <summary>
        /// Copies the contents of the given surface to the upper left corner of this surface.
        /// </summary>
        /// <param name="source">The surface to copy pixels from.</param>
        /// <remarks>
        /// The source surface does not need to have the same dimensions as this surface. Clipping
        /// will be handled automatically. No resizing will be done.
        /// </remarks>
        private void CopySurface(ShimSurfaceGray8 source)
        {
            if (stride == source.stride &&
                width == source.width &&
                height == source.height)
            {
                unsafe
                {
                    Memory.Copy(source.scan0.VoidStar,
                                scan0.VoidStar,
                                ((ulong)(height - 1) * (ulong)stride) + (ulong)width);
                }
            }
            else
            {
                int copyWidth = Math.Min(width, source.width);
                int copyHeight = Math.Min(height, source.height);

                unsafe
                {
                    for (int y = 0; y < copyHeight; ++y)
                    {
                        Memory.Copy(source.GetRowAddressUnchecked(y), GetRowAddressUnchecked(y), (ulong)copyWidth);
                    }
                }
            }
        }

        /// <summary>
        /// Fits the source surface to this surface using bicubic interpolation.
        /// </summary>
        /// <param name="source">The Surface to read pixels from.</param>
        /// <remarks>
        /// This method was implemented with correctness, not performance, in mind.
        /// Based on: "Bicubic Interpolation for Image Scaling" by Paul Bourke,
        ///           http://astronomy.swin.edu.au/%7Epbourke/colour/bicubic/
        /// </remarks>
        private void FitSurface(ShimSurfaceGray8 source)
        {
            float lastRowIndex = height - 1;
            float lastColumnIndex = width - 1;

            IntPtr srcColCachePtr = IntPtr.Zero;

            try
            {
                float* srcColCache;

                if (width > 128)
                {
                    srcColCachePtr = Memory.Allocate((long)width * sizeof(float));
                    srcColCache = (float*)srcColCachePtr;
                }
                else
                {
                    float* stackAllocPtr = stackalloc float[width];
                    srcColCache = stackAllocPtr;
                }

                // Precompute the source column indexes.
                for (int x = 0; x < width; x++)
                {
                    float u = x / lastColumnIndex;

                    srcColCache[x] = (u * source.Width) - 0.5f;
                }

                for (int y = 0; y < height; y++)
                {
                    byte* destRow = GetRowAddressUnchecked(y);
                    float v = y / lastRowIndex;

                    float srcY = (v * source.Height) - 0.5f;
                    int yint = (int)srcY;
                    float yfract = srcY - (float)Math.Floor(srcY);

                    for (int x = 0; x < width; x++)
                    {
                        float srcX = srcColCache[x];
                        int xint = (int)srcX;
                        float xfract = srcX - (float)Math.Floor(srcX);

                        // 1st row
                        byte p00 = *source.GetPointAddressClamped(xint - 1, yint - 1);
                        byte p10 = *source.GetPointAddressClamped(xint + 0, yint - 1);
                        byte p20 = *source.GetPointAddressClamped(xint + 1, yint - 1);
                        byte p30 = *source.GetPointAddressClamped(xint + 2, yint - 1);

                        // 2nd row
                        byte p01 = *source.GetPointAddressClamped(xint - 1, yint + 0);
                        byte p11 = *source.GetPointAddressClamped(xint + 0, yint + 0);
                        byte p21 = *source.GetPointAddressClamped(xint + 1, yint + 0);
                        byte p31 = *source.GetPointAddressClamped(xint + 2, yint + 0);

                        // 3rd row
                        byte p02 = *source.GetPointAddressClamped(xint - 1, yint + 1);
                        byte p12 = *source.GetPointAddressClamped(xint + 0, yint + 1);
                        byte p22 = *source.GetPointAddressClamped(xint + 1, yint + 1);
                        byte p32 = *source.GetPointAddressClamped(xint + 2, yint + 1);

                        // 4th row
                        byte p03 = *source.GetPointAddressClamped(xint - 1, yint + 2);
                        byte p13 = *source.GetPointAddressClamped(xint + 0, yint + 2);
                        byte p23 = *source.GetPointAddressClamped(xint + 1, yint + 2);
                        byte p33 = *source.GetPointAddressClamped(xint + 2, yint + 2);

                        float gray0 = BicubicUtil.CubicHermite(p00, p10, p20, p30, xfract);
                        float gray1 = BicubicUtil.CubicHermite(p01, p11, p21, p31, xfract);
                        float gray2 = BicubicUtil.CubicHermite(p02, p12, p22, p32, xfract);
                        float gray3 = BicubicUtil.CubicHermite(p03, p13, p23, p33, xfract);

                        float gray = BicubicUtil.CubicHermite(gray0, gray1, gray2, gray3, yfract);

                        if (gray < 0)
                        {
                            gray = 0;
                        }
                        else if (gray > 255)
                        {
                            gray = 255;
                        }

                        *destRow = (byte)gray;
                        destRow++;
                    }
                }
            }
            finally
            {
                if (srcColCachePtr != IntPtr.Zero)
                {
                    Memory.Free(srcColCachePtr);
                    srcColCachePtr = IntPtr.Zero;
                }
            }
        }

        private byte* GetPointAddressUnchecked(int x, int y)
        {
            return (((byte*)scan0.VoidStar + (y * stride)) + x);
        }

        private byte* GetPointAddressClamped(int x, int y)
        {
            if (x < 0)
            {
                x = 0;
            }
            else if (x >= width)
            {
                x = width - 1;
            }

            if (y < 0)
            {
                y = 0;
            }
            else if (y >= height)
            {
                y = height - 1;
            }

            return GetPointAddressUnchecked(x, y);
        }

        private byte* GetRowAddressUnchecked(int y)
        {
            return ((byte*)scan0.VoidStar + (y * stride));
        }
    }
}
