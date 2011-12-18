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

        public unsafe void FitSurface(Surface source)
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

        public unsafe Bitmap CreateAliasedBitmap()
        {
            return new Bitmap(width, height, stride, System.Drawing.Imaging.PixelFormat.Format32bppArgb, new IntPtr(this.scan0.VoidStar));
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
