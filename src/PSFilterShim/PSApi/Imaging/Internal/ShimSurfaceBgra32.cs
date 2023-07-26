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

using PaintDotNet;
using System;
using System.Drawing;

#nullable enable

namespace PSFilterLoad.PSApi.Imaging.Internal
{
    internal sealed unsafe class ShimSurfaceBgra32 : ImageSurface
    {
        private readonly int width;
        private readonly int height;
        private readonly int stride;
        private readonly MemoryBlock scan0;

        public ShimSurfaceBgra32(int width, int height)
        {
            this.width = width;
            this.height = height;

            try
            {
                stride = checked(width * sizeof(ColorBgra32));
            }
            catch (OverflowException ex)
            {
                throw new OutOfMemoryException("Dimensions are too large - not enough memory, width=" + width.ToString() + ", height=" + height.ToString(), ex);
            }

            scan0 = new MemoryBlock((long)height * stride);
        }

        public override int Width => width;

        public override int Height => height;

        public override int ChannelCount => 4;

        public override int BitsPerChannel => 8;

        public override SurfacePixelFormat Format => SurfacePixelFormat.Bgra32;

        public override bool SupportsTransparency => true;

        private Rectangle Bounds => new(0, 0, width, height);

        public override ShimSurfaceBgra32 Clone()
        {
            VerifyNotDisposed();

            ShimSurfaceBgra32 surf = new(width, height);
            surf.CopySurface(this);
            return surf;
        }

        public override ShimSurfaceBgra32 CreateScaledSurface(int newWidth, int newHeight)
        {
            VerifyNotDisposed();

            ShimSurfaceBgra32 surf = new(newWidth, newHeight);
            surf.BicubicFitSurface(this);
            return surf;
        }

        public override bool HasTransparency()
        {
            for (int y = 0; y < height; y++)
            {
                ColorBgra32* p = GetRowAddressUnchecked(y);
                for (int x = 0; x < width; x++)
                {
                    if (p->A < 255)
                    {
                        return true;
                    }

                    p++;
                }
            }

            return false;
        }

        public override ShimSurfaceBgra32Lock Lock(SurfaceLockMode mode)
        {
            return new(scan0.VoidStar, stride, width, height);
        }

        public override ShimSurfaceBgra32Lock Lock(Rectangle bounds, SurfaceLockMode mode)
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

        /// <summary>
        /// Copies the contents of the given surface to the upper left corner of this surface.
        /// </summary>
        /// <param name="source">The surface to copy pixels from.</param>
        /// <remarks>
        /// The source surface does not need to have the same dimensions as this surface. Clipping
        /// will be handled automatically. No resizing will be done.
        /// </remarks>
        private void CopySurface(ShimSurfaceBgra32 source)
        {
            if (stride == source.stride &&
                (width * sizeof(ColorBgra32)) == stride &&
                width == source.width &&
                height == source.height)
            {
                unsafe
                {
                    Memory.Copy(scan0.VoidStar,
                                source.scan0.VoidStar,
                                ((ulong)(height - 1) * (ulong)stride) + ((ulong)width * (ulong)sizeof(ColorBgra32)));
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
                        Memory.Copy(GetRowAddressUnchecked(y), source.GetRowAddressUnchecked(y), (ulong)copyWidth * (ulong)sizeof(ColorBgra32));
                    }
                }
            }
        }

        private ColorBgra32* GetPointAddressUnchecked(int x, int y)
        {
            return (ColorBgra32*)(((byte*)scan0.VoidStar) + (y * stride) + (x * sizeof(ColorBgra32)));
        }

        private ColorBgra32* GetRowAddressUnchecked(int y)
        {
            return (ColorBgra32*)(((byte*)scan0.VoidStar) + (y * stride));
        }

        /// <summary>
        /// Fits the source surface to this surface using bicubic interpolation.
        /// </summary>
        /// <param name="source">The Surface to read pixels from.</param>
        /// <param name="dstRoi">The rectangle to clip rendering to.</param>
        /// <remarks>
        /// This method was implemented with correctness, not performance, in mind.
        /// Based on: "Bicubic Interpolation for Image Scaling" by Paul Bourke,
        ///           http://astronomy.swin.edu.au/%7Epbourke/colour/bicubic/
        /// </remarks>
        public void BicubicFitSurface(ShimSurfaceBgra32 source)
        {
            float leftF = (1 * (float)(width - 1)) / (float)(source.width - 1);
            float topF = (1 * (height - 1)) / (float)(source.height - 1);
            float rightF = ((float)(source.width - 3) * (float)(width - 1)) / (float)(source.width - 1);
            float bottomF = ((float)(source.Height - 3) * (float)(height - 1)) / (float)(source.height - 1);

            int left = (int)Math.Ceiling((double)leftF);
            int top = (int)Math.Ceiling((double)topF);
            int right = (int)Math.Floor((double)rightF);
            int bottom = (int)Math.Floor((double)bottomF);

            Rectangle[] rois = new Rectangle[] {
                                                   Rectangle.FromLTRB(left, top, right, bottom),
                                                   new Rectangle(0, 0, width, top),
                                                   new Rectangle(0, top, left, height - top),
                                                   new Rectangle(right, top, width - right, height - top),
                                                   new Rectangle(left, bottom, right - left, height - bottom)
                                               };
            Rectangle dstRoi = Bounds;
            for (int i = 0; i < rois.Length; ++i)
            {
                rois[i].Intersect(dstRoi);

                if (rois[i].Width > 0 && rois[i].Height > 0)
                {
                    if (i == 0)
                    {
                        BicubicFitSurfaceUnchecked(source, rois[i]);
                    }
                    else
                    {
                        BicubicFitSurfaceChecked(source, rois[i]);
                    }
                }
            }
        }

        /// <summary>
        /// Implements bicubic filtering with bounds checking at every pixel.
        /// </summary>
        private void BicubicFitSurfaceChecked(ShimSurfaceBgra32 source, Rectangle dstRoi)
        {
            Rectangle roi = Rectangle.Intersect(dstRoi, Bounds);
            Rectangle roiIn = Rectangle.Intersect(dstRoi, new Rectangle(1, 1, width - 1, height - 1));

            IntPtr rColCacheIP = Memory.Allocate(4 * (long)roi.Width * sizeof(double));
            double* rColCache = (double*)rColCacheIP.ToPointer();

            // Precompute and then cache the value of R() for each column
            for (int dstX = roi.Left; dstX < roi.Right; ++dstX)
            {
                double srcColumn = dstX * (source.width - 1) / (double)(width - 1);
                double srcColumnFloor = Math.Floor(srcColumn);
                double srcColumnFrac = srcColumn - srcColumnFloor;
                for (int m = -1; m <= 2; ++m)
                {
                    int index = (m + 1) + ((dstX - roi.Left) * 4);
                    double x = m - srcColumnFrac;
                    rColCache[index] = BicubicUtil.R(x);
                }
            }

            // Set this up so we can cache the R()'s for every row
            double* rRowCache = stackalloc double[4];

            for (int dstY = roi.Top; dstY < roi.Bottom; ++dstY)
            {
                double srcRow = dstY * (source.height - 1) / (double)(height - 1);
                double srcRowFloor = (double)Math.Floor(srcRow);
                double srcRowFrac = srcRow - srcRowFloor;
                int srcRowInt = (int)srcRow;
                ColorBgra32* dstPtr = GetPointAddressUnchecked(roi.Left, dstY);

                // Compute the R() values for this row
                for (int n = -1; n <= 2; ++n)
                {
                    double x = srcRowFrac - n;
                    rRowCache[n + 1] = BicubicUtil.R(x);
                }

                // See Perf Note below
                //int nFirst = Math.Max(-srcRowInt, -1);
                //int nLast = Math.Min(source.height - srcRowInt - 1, 2);

                for (int dstX = roi.Left; dstX < roi.Right; dstX++)
                {
                    double srcColumn = dstX * (source.width - 1) / (double)(width - 1);
                    double srcColumnFloor = Math.Floor(srcColumn);
                    double srcColumnFrac = srcColumn - srcColumnFloor;
                    int srcColumnInt = (int)srcColumn;

                    double blueSum = 0;
                    double greenSum = 0;
                    double redSum = 0;
                    double alphaSum = 0;
                    double totalWeight = 0;

                    // See Perf Note below
                    //int mFirst = Math.Max(-srcColumnInt, -1);
                    //int mLast = Math.Min(source.width - srcColumnInt - 1, 2);

                    ColorBgra32* srcPtr = source.GetPointAddressUnchecked(srcColumnInt - 1, srcRowInt - 1);

                    for (int n = -1; n <= 2; ++n)
                    {
                        int srcY = srcRowInt + n;

                        for (int m = -1; m <= 2; ++m)
                        {
                            // Perf Note: It actually benchmarks faster on my system to do
                            // a bounds check for every (m,n) than it is to limit the loop
                            // to nFirst-Last and mFirst-mLast.
                            // I'm leaving the code above, albeit commented out, so that
                            // benchmarking between these two can still be performed.
                            if (source.IsVisible(srcColumnInt + m, srcY))
                            {
                                double w0 = rColCache[(m + 1) + (4 * (dstX - roi.Left))];
                                double w1 = rRowCache[n + 1];
                                double w = w0 * w1;

                                blueSum += srcPtr->B * w * srcPtr->A;
                                greenSum += srcPtr->G * w * srcPtr->A;
                                redSum += srcPtr->R * w * srcPtr->A;
                                alphaSum += srcPtr->A * w;

                                totalWeight += w;
                            }

                            ++srcPtr;
                        }

                        srcPtr = (ColorBgra32*)((byte*)(srcPtr - 4) + source.stride);
                    }

                    double alpha = alphaSum / totalWeight;
                    double blue;
                    double green;
                    double red;

                    if (alpha == 0)
                    {
                        blue = 0;
                        green = 0;
                        red = 0;
                    }
                    else
                    {
                        blue = blueSum / alphaSum;
                        green = greenSum / alphaSum;
                        red = redSum / alphaSum;

                        // add 0.5 to ensure truncation to uint results in rounding
                        alpha += 0.5;
                        blue += 0.5;
                        green += 0.5;
                        red += 0.5;
                    }

                    dstPtr->B = (byte)blue;
                    dstPtr->G = (byte)green;
                    dstPtr->R = (byte)red;
                    dstPtr->A = (byte)alpha;
                    ++dstPtr;
                } // for (dstX...
            } // for (dstY...

            Memory.Free(rColCacheIP);
        }

        /// <summary>
        /// Implements bicubic filtering with NO bounds checking at any pixel.
        /// </summary>
        private void BicubicFitSurfaceUnchecked(ShimSurfaceBgra32 source, Rectangle dstRoi)
        {
            Rectangle roi = Rectangle.Intersect(dstRoi, Bounds);
            Rectangle roiIn = Rectangle.Intersect(dstRoi, new Rectangle(1, 1, width - 1, height - 1));

            IntPtr rColCacheIP = Memory.Allocate(4 * (long)roi.Width * sizeof(double));
            double* rColCache = (double*)rColCacheIP.ToPointer();

            // Precompute and then cache the value of R() for each column
            for (int dstX = roi.Left; dstX < roi.Right; ++dstX)
            {
                double srcColumn = dstX * (source.width - 1) / (double)(width - 1);
                double srcColumnFloor = Math.Floor(srcColumn);
                double srcColumnFrac = srcColumn - srcColumnFloor;
                for (int m = -1; m <= 2; ++m)
                {
                    int index = (m + 1) + ((dstX - roi.Left) * 4);
                    double x = m - srcColumnFrac;
                    rColCache[index] = BicubicUtil.R(x);
                }
            }

            // Set this up so we can cache the R()'s for every row
            double* rRowCache = stackalloc double[4];

            for (int dstY = roi.Top; dstY < roi.Bottom; ++dstY)
            {
                double srcRow = dstY * (source.height - 1) / (double)(height - 1);
                double srcRowFloor = Math.Floor(srcRow);
                double srcRowFrac = srcRow - srcRowFloor;
                int srcRowInt = (int)srcRow;
                ColorBgra32* dstPtr = GetPointAddressUnchecked(roi.Left, dstY);

                // Compute the R() values for this row
                for (int n = -1; n <= 2; ++n)
                {
                    double x = srcRowFrac - n;
                    rRowCache[n + 1] = BicubicUtil.R(x);
                }

                rColCache = (double*)rColCacheIP.ToPointer();
                ColorBgra32* srcRowPtr = source.GetRowAddressUnchecked(srcRowInt - 1);

                for (int dstX = roi.Left; dstX < roi.Right; dstX++)
                {
                    double srcColumn = dstX * (source.width - 1) / (double)(width - 1);
                    double srcColumnFloor = Math.Floor(srcColumn);
                    double srcColumnFrac = srcColumn - srcColumnFloor;
                    int srcColumnInt = (int)srcColumn;

                    double blueSum = 0;
                    double greenSum = 0;
                    double redSum = 0;
                    double alphaSum = 0;
                    double totalWeight = 0;

                    ColorBgra32* srcPtr = srcRowPtr + srcColumnInt - 1;
                    for (int n = 0; n <= 3; ++n)
                    {
                        double w0 = rColCache[0] * rRowCache[n];
                        double w1 = rColCache[1] * rRowCache[n];
                        double w2 = rColCache[2] * rRowCache[n];
                        double w3 = rColCache[3] * rRowCache[n];

                        double a0 = srcPtr[0].A;
                        double a1 = srcPtr[1].A;
                        double a2 = srcPtr[2].A;
                        double a3 = srcPtr[3].A;

                        alphaSum += (a0 * w0) + (a1 * w1) + (a2 * w2) + (a3 * w3);
                        totalWeight += w0 + w1 + w2 + w3;

                        blueSum += (a0 * srcPtr[0].B * w0) + (a1 * srcPtr[1].B * w1) + (a2 * srcPtr[2].B * w2) + (a3 * srcPtr[3].B * w3);
                        greenSum += (a0 * srcPtr[0].G * w0) + (a1 * srcPtr[1].G * w1) + (a2 * srcPtr[2].G * w2) + (a3 * srcPtr[3].G * w3);
                        redSum += (a0 * srcPtr[0].R * w0) + (a1 * srcPtr[1].R * w1) + (a2 * srcPtr[2].R * w2) + (a3 * srcPtr[3].R * w3);

                        srcPtr = (ColorBgra32*)((byte*)srcPtr + source.stride);
                    }

                    double alpha = alphaSum / totalWeight;

                    double blue;
                    double green;
                    double red;

                    if (alpha == 0)
                    {
                        blue = 0;
                        green = 0;
                        red = 0;
                    }
                    else
                    {
                        blue = blueSum / alphaSum;
                        green = greenSum / alphaSum;
                        red = redSum / alphaSum;

                        // add 0.5 to ensure truncation to uint results in rounding
                        alpha += 0.5;
                        blue += 0.5;
                        green += 0.5;
                        red += 0.5;
                    }

                    dstPtr->B = (byte)blue;
                    dstPtr->G = (byte)green;
                    dstPtr->R = (byte)red;
                    dstPtr->A = (byte)alpha;

                    ++dstPtr;
                    rColCache += 4;
                } // for (dstX...
            } // for (dstY...

            Memory.Free(rColCacheIP);
        }

        private bool IsVisible(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
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
