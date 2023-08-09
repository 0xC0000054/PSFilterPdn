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

using PaintDotNet;
using System;
using System.Drawing;
using System.Runtime.InteropServices;

#nullable enable

namespace PSFilterLoad.PSApi.Imaging.Internal
{
    internal sealed unsafe class ShimSurfaceBgr24 : ImageSurface
    {
        private readonly int width;
        private readonly int height;
        private readonly int stride;
        private readonly MemoryBlock scan0;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShimSurfaceBgr24"/> class.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="fourByteAlignedStride">
        /// <see langword="true"/> to pad the stride to a multiple of 4 bytes; otherwise, <see langword="false"/>.
        /// This is required for compatibility with GDI+.
        /// </param>
        /// <exception cref="System.OutOfMemoryException">Dimensions are too large - not enough memory, width=" + width.ToString() + ", height=" + height.ToString()</exception>
        public ShimSurfaceBgr24(int width, int height, bool fourByteAlignedStride = false)
        {
            this.width = width;
            this.height = height;

            try
            {
                checked
                {
                    stride = width * sizeof(ColorBgr24);

                    if (fourByteAlignedStride)
                    {
                        stride = checked((stride + 3) & ~3);
                    }
                }
            }
            catch (OverflowException ex)
            {
                throw new OutOfMemoryException("Dimensions are too large - not enough memory, width=" + width.ToString() + ", height=" + height.ToString(), ex);
            }

            scan0 = new MemoryBlock((long)height * stride);
        }

        public override int Width => width;

        public override int Height => height;

        public override int ChannelCount => 3;

        public override int BitsPerChannel => 8;

        public override SurfacePixelFormat Format => SurfacePixelFormat.Bgr24;

        public override bool SupportsTransparency => false;

        public override ISurface<ImageSurface> Clone()
        {
            VerifyNotDisposed();

            ShimSurfaceBgr24 surface = new(width, height);
            surface.CopySurface(this);
            return surface;
        }

        public override ISurface<ImageSurface> CreateScaledSurface(int newWidth, int newHeight)
        {
            VerifyNotDisposed();

            ShimSurfaceBgr24 surface = new(newWidth, newHeight);
            surface.FitSurface(this);
            return surface;
        }

        public override ShimSurfaceBgr24Lock Lock(SurfaceLockMode mode)
        {
            return new(scan0.VoidStar, stride, width, height);
        }

        public override ShimSurfaceBgr24Lock Lock(Rectangle bounds, SurfaceLockMode mode)
        {
            Rectangle original = new(0, 0, width, height);
            Rectangle sub = bounds;
            Rectangle clipped = Rectangle.Intersect(original, sub);

            if (clipped != sub)
            {
                throw new ArgumentOutOfRangeException(nameof(bounds), bounds,
                    "bounds parameters must be a subset of this Surface's bounds");
            }

            void* buffer = GetPointAddressUnchecked(bounds.X, bounds.Y);

            return new(buffer, stride, bounds.Width, bounds.Height);
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
        private unsafe void CopySurface(ShimSurfaceBgr24 source)
        {
            if (stride == source.stride &&
                (width * sizeof(ColorBgr24)) == stride &&
                width == source.width &&
                height == source.height)
            {
                NativeMemory.Copy(source.scan0.VoidStar,
                                  scan0.VoidStar,
                                  checked(((nuint)(height - 1) * (nuint)stride) + ((nuint)width * (nuint)sizeof(ColorBgr24))));
            }
            else
            {
                int copyWidth = Math.Min(width, source.width);
                int copyHeight = Math.Min(height, source.height);

                for (int y = 0; y < copyHeight; ++y)
                {
                    NativeMemory.Copy(source.GetRowAddressUnchecked(y),
                                      GetRowAddressUnchecked(y),
                                      (nuint)copyWidth * (nuint)sizeof(ColorBgr24));
                }
            }
        }

        private unsafe void FitSurface(ShimSurfaceBgr24 source)
        {
            float lastRowIndex = height - 1;
            float lastColumnIndex = width - 1;

            IntPtr srcColCachePtr = IntPtr.Zero;

            try
            {
                srcColCachePtr = Memory.Allocate((long)width * sizeof(float));
                float* srcColCache = (float*)srcColCachePtr;

                // Precompute the source column indexes.
                for (int x = 0; x < width; x++)
                {
                    float u = x / lastColumnIndex;

                    srcColCache[x] = (u * source.Width) - 0.5f;
                }

                for (int y = 0; y < height; y++)
                {
                    ColorBgr24* destRow = GetRowAddressUnchecked(y);
                    float v = y / lastRowIndex;

                    float srcY = (v * source.Height) - 0.5f;
                    int yint = (int)srcY;
                    float yfract = srcY - MathF.Floor(srcY);

                    for (int x = 0; x < width; x++)
                    {
                        float srcX = srcColCache[x];
                        int xint = (int)srcX;
                        float xfract = srcX - MathF.Floor(srcX);

                        // 1st row
                        ColorBgr24 p00 = *source.GetPointAddressClamped(xint - 1, yint - 1);
                        ColorBgr24 p10 = *source.GetPointAddressClamped(xint + 0, yint - 1);
                        ColorBgr24 p20 = *source.GetPointAddressClamped(xint + 1, yint - 1);
                        ColorBgr24 p30 = *source.GetPointAddressClamped(xint + 2, yint - 1);

                        // 2nd row
                        ColorBgr24 p01 = *source.GetPointAddressClamped(xint - 1, yint + 0);
                        ColorBgr24 p11 = *source.GetPointAddressClamped(xint + 0, yint + 0);
                        ColorBgr24 p21 = *source.GetPointAddressClamped(xint + 1, yint + 0);
                        ColorBgr24 p31 = *source.GetPointAddressClamped(xint + 2, yint + 0);

                        // 3rd row
                        ColorBgr24 p02 = *source.GetPointAddressClamped(xint - 1, yint + 1);
                        ColorBgr24 p12 = *source.GetPointAddressClamped(xint + 0, yint + 1);
                        ColorBgr24 p22 = *source.GetPointAddressClamped(xint + 1, yint + 1);
                        ColorBgr24 p32 = *source.GetPointAddressClamped(xint + 2, yint + 1);

                        // 4th row
                        ColorBgr24 p03 = *source.GetPointAddressClamped(xint - 1, yint + 2);
                        ColorBgr24 p13 = *source.GetPointAddressClamped(xint + 0, yint + 2);
                        ColorBgr24 p23 = *source.GetPointAddressClamped(xint + 1, yint + 2);
                        ColorBgr24 p33 = *source.GetPointAddressClamped(xint + 2, yint + 2);

                        float blue0 = BicubicUtil.CubicHermite(p00.B, p10.B, p20.B, p30.B, xfract);
                        float blue1 = BicubicUtil.CubicHermite(p01.B, p11.B, p21.B, p31.B, xfract);
                        float blue2 = BicubicUtil.CubicHermite(p02.B, p12.B, p22.B, p32.B, xfract);
                        float blue3 = BicubicUtil.CubicHermite(p03.B, p13.B, p23.B, p33.B, xfract);

                        float blue = BicubicUtil.CubicHermite(blue0, blue1, blue2, blue3, yfract);

                        float green0 = BicubicUtil.CubicHermite(p00.G, p10.G, p20.G, p30.G, xfract);
                        float green1 = BicubicUtil.CubicHermite(p01.G, p11.G, p21.G, p31.G, xfract);
                        float green2 = BicubicUtil.CubicHermite(p02.G, p12.G, p22.G, p32.G, xfract);
                        float green3 = BicubicUtil.CubicHermite(p03.G, p13.G, p23.G, p33.G, xfract);

                        float green = BicubicUtil.CubicHermite(green0, green1, green2, green3, yfract);

                        float red0 = BicubicUtil.CubicHermite(p00.R, p10.R, p20.R, p30.R, xfract);
                        float red1 = BicubicUtil.CubicHermite(p01.R, p11.R, p21.R, p31.R, xfract);
                        float red2 = BicubicUtil.CubicHermite(p02.R, p12.R, p22.R, p32.R, xfract);
                        float red3 = BicubicUtil.CubicHermite(p03.R, p13.R, p23.R, p33.R, xfract);

                        float red = BicubicUtil.CubicHermite(red0, red1, red2, red3, yfract);

                        destRow->B = (byte)Math.Clamp(blue, 0, 255);
                        destRow->G = (byte)Math.Clamp(green, 0, 255);
                        destRow->R = (byte)Math.Clamp(red, 0, 255);
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

        private ColorBgr24* GetPointAddressClamped(int x, int y)
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

        private ColorBgr24* GetPointAddressUnchecked(int x, int y)
            => (ColorBgr24*)(((byte*)scan0.VoidStar) + ((long)y * stride) + ((long)x * sizeof(ColorBgr24)));

        private ColorBgr24* GetRowAddressUnchecked(int y) => (ColorBgr24*)(((byte*)scan0.VoidStar) + ((long)y * stride));
    }
}
