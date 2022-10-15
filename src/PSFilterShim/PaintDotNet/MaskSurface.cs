/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2022 Nicholas Hayes
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
using PaintDotNet.SystemLayer;

namespace PaintDotNet
{
    /// <summary>
    /// An 8-bit per pixel surface used for the selection mask.
    /// </summary>
    internal sealed class MaskSurface : IDisposable
    {
        private int width;
        private int height;
        private int stride;
        private MemoryBlock scan0;
        private bool disposed;

        public MaskSurface(int width, int height)
            : this(width, height, zeroFill: true)
        {
        }

        private MaskSurface(int width ,int height, bool zeroFill)
        {
            disposed = false;
            this.width = width;
            this.height = height;
            stride = width;
            scan0 = new MemoryBlock(width * height, zeroFill);
        }

        /// <summary>
        /// Gets the width of the Surface.
        /// </summary>
        public int Width
        {
            get
            {
                return width;
            }
        }

        /// <summary>
        /// Gets the height of the Surface.
        /// </summary>
        public int Height
        {
            get
            {
                return height;
            }
        }

        /// <summary>
        /// Gets the stride of the Surface.
        /// </summary>
        public int Stride
        {
            get
            {
                return stride;
            }
        }

        public MemoryBlock Scan0
        {
            get
            {
                return scan0;
            }
        }

        public Rectangle Bounds
        {
            get
            {
                return new Rectangle(0, 0, width, height);
            }
        }

        public MaskSurface Clone()
        {
            MaskSurface surface = new(width, height, zeroFill: false);
            surface.CopySurface(this);
            return surface;
        }

        /// <summary>
        /// Copies the contents of the given surface to the upper left corner of this surface.
        /// </summary>
        /// <param name="source">The surface to copy pixels from.</param>
        /// <remarks>
        /// The source surface does not need to have the same dimensions as this surface. Clipping
        /// will be handled automatically. No resizing will be done.
        /// </remarks>
        private void CopySurface(MaskSurface source)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("Surface");
            }

            if (stride == source.stride &&
                width == source.width &&
                height == source.height)
            {
                unsafe
                {
                    Memory.Copy(scan0.VoidStar,
                                source.scan0.VoidStar,
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
                        Memory.Copy(GetRowAddressUnchecked(y), source.GetRowAddressUnchecked(y), (ulong)copyWidth);
                    }
                }
            }
        }

        public byte GetPoint(int x, int y)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
            {
                throw new ArgumentOutOfRangeException("(x,y)", new Point(x, y), "Coordinates out of range, max=" + new Size(width - 1, height - 1).ToString());
            }

            return GetPointUnchecked(x, y);
        }

        public byte GetPointUnchecked(int x, int y)
        {
            unsafe
            {
                return *GetPointAddressUnchecked(x, y);
            }
        }

        public unsafe byte* GetPointAddressUnchecked(int x, int y)
        {
            return (((byte*)scan0.VoidStar + (y * stride)) + x);
        }

        public unsafe byte* GetRowAddressUnchecked(int y)
        {
            return ((byte*)scan0.VoidStar + (y * stride));
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
        public unsafe void FitSurface(MaskSurface source)
        {
            float lastRowIndex = height - 1;
            float lastColumnIndex = width - 1;

            IntPtr srcColCachePtr = IntPtr.Zero;

            try
            {
                float* srcColCache;

                if (width > 128)
                {
                    srcColCachePtr = Memory.Allocate((ulong)width * sizeof(float));
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

                        float gray0 = CubicHermite(p00, p10, p20, p30, xfract);
                        float gray1 = CubicHermite(p01, p11, p21, p31, xfract);
                        float gray2 = CubicHermite(p02, p12, p22, p32, xfract);
                        float gray3 = CubicHermite(p03, p13, p23, p33, xfract);

                        float gray = CubicHermite(gray0, gray1, gray2, gray3, yfract);

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

        // From https://blog.demofox.org/2015/08/15/resizing-images-with-bicubic-interpolation/
        // t is a value that goes from 0 to 1 to interpolate in a C1 continuous way across uniformly sampled data points.
        // when t is 0, this will return B.  When t is 1, this will return C. Inbetween values will return an interpolation
        // between B and C.  A and B are used to calculate slopes at the edges.
        private static float CubicHermite(float A, float B, float C, float D, float t)
        {
            float a = -A / 2.0f + (3.0f * B) / 2.0f - (3.0f * C) / 2.0f + D / 2.0f;
            float b = A - (5.0f * B) / 2.0f + 2.0f * C - D / 2.0f;
            float c = -A / 2.0f + C / 2.0f;
            float d = B;

            return a * t * t * t + b * t * t + c * t + d;
        }

        private unsafe byte* GetPointAddressClamped(int x, int y)
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

        /// <summary>
        /// Determines if the requested pixel coordinate is within bounds.
        /// </summary>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        /// <returns>true if (x,y) is in bounds, false if it's not.</returns>
        public bool IsVisible(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        private void Dispose(bool disposing)
        {
            if (!disposed && disposing)
            {
                disposed = true;
                if (scan0 != null)
                {
                    scan0.Dispose();
                    scan0 = null;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
