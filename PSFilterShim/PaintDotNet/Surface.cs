using System;
using System.Drawing;
using System.Drawing.Imaging;
using PaintDotNet.SystemLayer;

namespace PaintDotNet
{
    /////////////////////////////////////////////////////////////////////////////////
    // Paint.NET                                                                   //
    // Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
    // Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
    // See src/Resources/Files/License.txt for full licensing and attribution      //
    // details.                                                                    //
    // .                                                                           //
    /////////////////////////////////////////////////////////////////////////////////

    internal sealed class Surface : IDisposable
    {
        private int width;
        private int height;
        private int stride;
        private MemoryBlock scan0;
        private bool disposed;

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
        public long Stride
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
                return new Rectangle(0, 0, this.width, this.height);
            }
        }

        public Surface(int width, int height)
        {
            this.width = width;
            this.height = height;
            this.stride = width * 4;
            long bytes = (long)(width * height * 4);
            scan0 = new MemoryBlock(bytes);
            this.disposed = false;
        }

        public Surface Clone()
        {
            Surface surf = new Surface(width, height);
            surf.CopySurface(this);
            return surf;
        }

        /// <summary>
        /// Creates a new Surface and copies the pixels from a Bitmap to it.
        /// </summary>
        /// <param name="bitmap">The Bitmap to duplicate.</param>
        /// <returns>A new Surface that is the same size as the given Bitmap and that has the same pixel values.</returns>
        public static Surface CopyFromBitmap(Bitmap bitmap)
        {
            Surface surface = new Surface(bitmap.Width, bitmap.Height);
            BitmapData bd = bitmap.LockBits(surface.Bounds, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            unsafe
            {
                for (int y = 0; y < bd.Height; ++y)
                {
                    Memory.Copy((void*)surface.GetRowAddressUnchecked(y),
                        (byte*)bd.Scan0.ToPointer() + (y * bd.Stride), (ulong)bd.Width * ColorBgra.SizeOf);
                }
            }

            bitmap.UnlockBits(bd);
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
        public void CopySurface(Surface source)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("Surface");
            }

            if (this.stride == source.stride &&
                (this.width * ColorBgra.SizeOf) == this.stride &&
                this.width == source.width &&
                this.height == source.height)
            {
                unsafe
                {
                    Memory.Copy(this.scan0.VoidStar,
                                source.scan0.VoidStar,
                                ((ulong)(height - 1) * (ulong)stride) + ((ulong)width * (ulong)ColorBgra.SizeOf));
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
                        Memory.Copy(GetRowAddressUnchecked(y), source.GetRowAddressUnchecked(y), (ulong)copyWidth * (ulong)ColorBgra.SizeOf);
                    }
                }
            }
        }

        public ColorBgra GetPoint(int x, int y)
        {
            if (x < 0 || y < 0 || x >= this.width || y >= this.height)
            {
                throw new ArgumentOutOfRangeException("(x,y)", new Point(x, y), "Coordinates out of range, max=" + new Size(width - 1, height - 1).ToString());
            }

            return GetPointUnchecked(x, y);
        }

        public ColorBgra GetPointUnchecked(int x, int y)
        {
            unsafe
            {
                return *GetPointAddressUnchecked(x, y);
            }
        }

        public unsafe ColorBgra* GetPointAddressUnchecked(int x, int y)
        {
            return (ColorBgra*)(((byte*)scan0.VoidStar) + (y * stride) + (x * ColorBgra.SizeOf));
        }

        public unsafe ColorBgra* GetRowAddressUnchecked(int y)
        {
            return (ColorBgra*)(((byte*)scan0.VoidStar) + (y * stride));
        }

        private double CubeClamped(double x)
        {
            if (x >= 0)
            {
                return x * x * x;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Implements R() as defined at http://astronomy.swin.edu.au/%7Epbourke/colour/bicubic/
        /// </summary>
        private double R(double x)
        {
            return (CubeClamped(x + 2) - (4 * CubeClamped(x + 1)) + (6 * CubeClamped(x)) - (4 * CubeClamped(x - 1))) / 6;
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
        public void BicubicFitSurface(Surface source)
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
            Rectangle dstRoi = this.Bounds;
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
        private unsafe void BicubicFitSurfaceChecked(Surface source, Rectangle dstRoi)
        {
            Rectangle roi = Rectangle.Intersect(dstRoi, this.Bounds);
            Rectangle roiIn = Rectangle.Intersect(dstRoi, new Rectangle(1, 1, width - 1, height - 1));

            IntPtr rColCacheIP = Memory.Allocate(4 * (ulong)roi.Width * (ulong)sizeof(double));
            double* rColCache = (double*)rColCacheIP.ToPointer();

            // Precompute and then cache the value of R() for each column
            for (int dstX = roi.Left; dstX < roi.Right; ++dstX)
            {
                double srcColumn = (double)(dstX * (source.width - 1)) / (double)(width - 1);
                double srcColumnFloor = Math.Floor(srcColumn);
                double srcColumnFrac = srcColumn - srcColumnFloor;
                for (int m = -1; m <= 2; ++m)
                {
                    int index = (m + 1) + ((dstX - roi.Left) * 4);
                    double x = m - srcColumnFrac;
                    rColCache[index] = R(x);
                }
            }

            // Set this up so we can cache the R()'s for every row
            double* rRowCache = stackalloc double[4];

            for (int dstY = roi.Top; dstY < roi.Bottom; ++dstY)
            {
                double srcRow = (double)(dstY * (source.height - 1)) / (double)(height - 1);
                double srcRowFloor = (double)Math.Floor(srcRow);
                double srcRowFrac = srcRow - srcRowFloor;
                int srcRowInt = (int)srcRow;
                ColorBgra* dstPtr = this.GetPointAddressUnchecked(roi.Left, dstY);

                // Compute the R() values for this row
                for (int n = -1; n <= 2; ++n)
                {
                    double x = srcRowFrac - n;
                    rRowCache[n + 1] = R(x);
                }

                // See Perf Note below
                //int nFirst = Math.Max(-srcRowInt, -1);
                //int nLast = Math.Min(source.height - srcRowInt - 1, 2);

                for (int dstX = roi.Left; dstX < roi.Right; dstX++)
                {
                    double srcColumn = (double)(dstX * (source.width - 1)) / (double)(width - 1);
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

                    ColorBgra* srcPtr = source.GetPointAddressUnchecked(srcColumnInt - 1, srcRowInt - 1);

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

                        srcPtr = (ColorBgra*)((byte*)(srcPtr - 4) + source.stride);
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

                    dstPtr->Bgra = (uint)blue + ((uint)green << 8) + ((uint)red << 16) + ((uint)alpha << 24);
                    ++dstPtr;
                } // for (dstX...
            } // for (dstY...

            Memory.Free(rColCacheIP);
        }

        /// <summary>
        /// Implements bicubic filtering with NO bounds checking at any pixel.
        /// </summary>
        private unsafe void BicubicFitSurfaceUnchecked(Surface source, Rectangle dstRoi)
        {
            Rectangle roi = Rectangle.Intersect(dstRoi, this.Bounds);
            Rectangle roiIn = Rectangle.Intersect(dstRoi, new Rectangle(1, 1, width - 1, height - 1));

            IntPtr rColCacheIP = Memory.Allocate(4 * (ulong)roi.Width * (ulong)sizeof(double));
            double* rColCache = (double*)rColCacheIP.ToPointer();

            // Precompute and then cache the value of R() for each column
            for (int dstX = roi.Left; dstX < roi.Right; ++dstX)
            {
                double srcColumn = (double)(dstX * (source.width - 1)) / (double)(width - 1);
                double srcColumnFloor = Math.Floor(srcColumn);
                double srcColumnFrac = srcColumn - srcColumnFloor;
                for (int m = -1; m <= 2; ++m)
                {
                    int index = (m + 1) + ((dstX - roi.Left) * 4);
                    double x = m - srcColumnFrac;
                    rColCache[index] = R(x);
                }
            }

            // Set this up so we can cache the R()'s for every row
            double* rRowCache = stackalloc double[4];

            for (int dstY = roi.Top; dstY < roi.Bottom; ++dstY)
            {
                double srcRow = (double)(dstY * (source.height - 1)) / (double)(height - 1);
                double srcRowFloor = Math.Floor(srcRow);
                double srcRowFrac = srcRow - srcRowFloor;
                int srcRowInt = (int)srcRow;
                ColorBgra* dstPtr = this.GetPointAddressUnchecked(roi.Left, dstY);

                // Compute the R() values for this row
                for (int n = -1; n <= 2; ++n)
                {
                    double x = srcRowFrac - n;
                    rRowCache[n + 1] = R(x);
                }

                rColCache = (double*)rColCacheIP.ToPointer();
                ColorBgra* srcRowPtr = source.GetRowAddressUnchecked(srcRowInt - 1);

                for (int dstX = roi.Left; dstX < roi.Right; dstX++)
                {
                    double srcColumn = (double)(dstX * (source.width - 1)) / (double)(width - 1);
                    double srcColumnFloor = Math.Floor(srcColumn);
                    double srcColumnFrac = srcColumn - srcColumnFloor;
                    int srcColumnInt = (int)srcColumn;

                    double blueSum = 0;
                    double greenSum = 0;
                    double redSum = 0;
                    double alphaSum = 0;
                    double totalWeight = 0;

                    ColorBgra* srcPtr = srcRowPtr + srcColumnInt - 1;
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

                        srcPtr = (ColorBgra*)((byte*)srcPtr + source.stride);
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

                    dstPtr->Bgra = (uint)blue + ((uint)green << 8) + ((uint)red << 16) + ((uint)alpha << 24);
                    ++dstPtr;
                    rColCache += 4;
                } // for (dstX...
            } // for (dstY...

            Memory.Free(rColCacheIP);
        }

        public unsafe void SuperSampleFitSurface(Surface source)
        {
            Rectangle dstRoi2 = Rectangle.Intersect(source.Bounds, this.Bounds);

            for (int dstY = dstRoi2.Top; dstY < dstRoi2.Bottom; ++dstY)
            {
                double srcTop = (double)(dstY * source.height) / (double)height;
                double srcTopFloor = Math.Floor(srcTop);
                double srcTopWeight = 1 - (srcTop - srcTopFloor);
                int srcTopInt = (int)srcTopFloor;

                double srcBottom = (double)((dstY + 1) * source.height) / (double)height;
                double srcBottomFloor = Math.Floor(srcBottom - 0.00001);
                double srcBottomWeight = srcBottom - srcBottomFloor;
                int srcBottomInt = (int)srcBottomFloor;

                ColorBgra* dstPtr = this.GetPointAddressUnchecked(dstRoi2.Left, dstY);

                for (int dstX = dstRoi2.Left; dstX < dstRoi2.Right; ++dstX)
                {
                    double srcLeft = (double)(dstX * source.width) / (double)width;
                    double srcLeftFloor = Math.Floor(srcLeft);
                    double srcLeftWeight = 1 - (srcLeft - srcLeftFloor);
                    int srcLeftInt = (int)srcLeftFloor;

                    double srcRight = (double)((dstX + 1) * source.width) / (double)width;
                    double srcRightFloor = Math.Floor(srcRight - 0.00001);
                    double srcRightWeight = srcRight - srcRightFloor;
                    int srcRightInt = (int)srcRightFloor;

                    double blueSum = 0;
                    double greenSum = 0;
                    double redSum = 0;
                    double alphaSum = 0;

                    // left fractional edge
                    ColorBgra* srcLeftPtr = source.GetPointAddressUnchecked(srcLeftInt, srcTopInt + 1);

                    for (int srcY = srcTopInt + 1; srcY < srcBottomInt; ++srcY)
                    {
                        double a = srcLeftPtr->A;
                        blueSum += srcLeftPtr->B * srcLeftWeight * a;
                        greenSum += srcLeftPtr->G * srcLeftWeight * a;
                        redSum += srcLeftPtr->R * srcLeftWeight * a;
                        alphaSum += srcLeftPtr->A * srcLeftWeight;
                        srcLeftPtr = (ColorBgra*)((byte*)srcLeftPtr + source.stride);
                    }

                    // right fractional edge
                    ColorBgra* srcRightPtr = source.GetPointAddressUnchecked(srcRightInt, srcTopInt + 1);
                    for (int srcY = srcTopInt + 1; srcY < srcBottomInt; ++srcY)
                    {
                        double a = srcRightPtr->A;
                        blueSum += srcRightPtr->B * srcRightWeight * a;
                        greenSum += srcRightPtr->G * srcRightWeight * a;
                        redSum += srcRightPtr->R * srcRightWeight * a;
                        alphaSum += srcRightPtr->A * srcRightWeight;
                        srcRightPtr = (ColorBgra*)((byte*)srcRightPtr + source.stride);
                    }

                    // top fractional edge
                    ColorBgra* srcTopPtr = source.GetPointAddressUnchecked(srcLeftInt + 1, srcTopInt);
                    for (int srcX = srcLeftInt + 1; srcX < srcRightInt; ++srcX)
                    {
                        double a = srcTopPtr->A;
                        blueSum += srcTopPtr->B * srcTopWeight * a;
                        greenSum += srcTopPtr->G * srcTopWeight * a;
                        redSum += srcTopPtr->R * srcTopWeight * a;
                        alphaSum += srcTopPtr->A * srcTopWeight;
                        ++srcTopPtr;
                    }

                    // bottom fractional edge
                    ColorBgra* srcBottomPtr = source.GetPointAddressUnchecked(srcLeftInt + 1, srcBottomInt);
                    for (int srcX = srcLeftInt + 1; srcX < srcRightInt; ++srcX)
                    {
                        double a = srcBottomPtr->A;
                        blueSum += srcBottomPtr->B * srcBottomWeight * a;
                        greenSum += srcBottomPtr->G * srcBottomWeight * a;
                        redSum += srcBottomPtr->R * srcBottomWeight * a;
                        alphaSum += srcBottomPtr->A * srcBottomWeight;
                        ++srcBottomPtr;
                    }

                    // center area
                    for (int srcY = srcTopInt + 1; srcY < srcBottomInt; ++srcY)
                    {
                        ColorBgra* srcPtr = source.GetPointAddressUnchecked(srcLeftInt + 1, srcY);

                        for (int srcX = srcLeftInt + 1; srcX < srcRightInt; ++srcX)
                        {
                            double a = srcPtr->A;
                            blueSum += (double)srcPtr->B * a;
                            greenSum += (double)srcPtr->G * a;
                            redSum += (double)srcPtr->R * a;
                            alphaSum += (double)srcPtr->A;
                            ++srcPtr;
                        }
                    }

                    // four corner pixels
                    ColorBgra srcTL = source.GetPoint(srcLeftInt, srcTopInt);
                    double srcTLA = srcTL.A;
                    blueSum += srcTL.B * (srcTopWeight * srcLeftWeight) * srcTLA;
                    greenSum += srcTL.G * (srcTopWeight * srcLeftWeight) * srcTLA;
                    redSum += srcTL.R * (srcTopWeight * srcLeftWeight) * srcTLA;
                    alphaSum += srcTL.A * (srcTopWeight * srcLeftWeight);

                    ColorBgra srcTR = source.GetPoint(srcRightInt, srcTopInt);
                    double srcTRA = srcTR.A;
                    blueSum += srcTR.B * (srcTopWeight * srcRightWeight) * srcTRA;
                    greenSum += srcTR.G * (srcTopWeight * srcRightWeight) * srcTRA;
                    redSum += srcTR.R * (srcTopWeight * srcRightWeight) * srcTRA;
                    alphaSum += srcTR.A * (srcTopWeight * srcRightWeight);

                    ColorBgra srcBL = source.GetPoint(srcLeftInt, srcBottomInt);
                    double srcBLA = srcBL.A;
                    blueSum += srcBL.B * (srcBottomWeight * srcLeftWeight) * srcBLA;
                    greenSum += srcBL.G * (srcBottomWeight * srcLeftWeight) * srcBLA;
                    redSum += srcBL.R * (srcBottomWeight * srcLeftWeight) * srcBLA;
                    alphaSum += srcBL.A * (srcBottomWeight * srcLeftWeight);

                    ColorBgra srcBR = source.GetPoint(srcRightInt, srcBottomInt);
                    double srcBRA = srcBR.A;
                    blueSum += srcBR.B * (srcBottomWeight * srcRightWeight) * srcBRA;
                    greenSum += srcBR.G * (srcBottomWeight * srcRightWeight) * srcBRA;
                    redSum += srcBR.R * (srcBottomWeight * srcRightWeight) * srcBRA;
                    alphaSum += srcBR.A * (srcBottomWeight * srcRightWeight);

                    double area = (srcRight - srcLeft) * (srcBottom - srcTop);

                    double alpha = alphaSum / area;
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
                    }

                    // add 0.5 so that rounding goes in the direction we want it to
                    blue += 0.5;
                    green += 0.5;
                    red += 0.5;
                    alpha += 0.5;

                    dstPtr->Bgra = (uint)blue + ((uint)green << 8) + ((uint)red << 16) + ((uint)alpha << 24);
                    ++dstPtr;
                }
            }
        }

        public unsafe void SetAlphaTo255()
        {
            for (int y = 0; y < height; y++)
            {
                ColorBgra* p = GetRowAddressUnchecked(y);
                for (int x = 0; x < width; x++)
                {
                    p->Bgra |= 0xff000000; // 0xaarrggbb format
                    p++;
                }
            }
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

        public unsafe Bitmap CreateAliasedBitmap()
        {
            return new Bitmap(width, height, stride, System.Drawing.Imaging.PixelFormat.Format32bppArgb, new IntPtr(this.scan0.VoidStar));
        }
        /// <summary>
        /// Gets or sets the pixel value at the requested offset.
        /// </summary>
        /// <remarks>
        /// This property is implemented with correctness and error checking in mind. If performance
        /// is a concern, do not use it.
        /// </remarks>
        public ColorBgra this[int x, int y]
        {
            get
            {
                if (disposed)
                {
                    throw new ObjectDisposedException("Surface");
                }

                if (x < 0 || y < 0 || x >= this.width || y >= this.height)
                {
                    throw new ArgumentOutOfRangeException("(x,y)", new Point(x, y), "Coordinates out of range, max=" + new Size(width - 1, height - 1).ToString());
                }

                unsafe
                {
                    return *GetPointAddressUnchecked(x, y);
                }
            }

            set
            {
                if (disposed)
                {
                    throw new ObjectDisposedException("Surface");
                }

                if (x < 0 || y < 0 || x >= this.width || y >= this.height)
                {
                    throw new ArgumentOutOfRangeException("(x,y)", new Point(x, y), "Coordinates out of range, max=" + new Size(width - 1, height - 1).ToString());
                }

                unsafe
                {
                    *GetPointAddressUnchecked(x, y) = value;
                }
            }
        }

        private void Dispose(bool disposing)
        {
            if (!disposed && disposing)
            {
                if (scan0 != null)
                {
                    this.scan0.Dispose();
                    this.scan0 = null;
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
