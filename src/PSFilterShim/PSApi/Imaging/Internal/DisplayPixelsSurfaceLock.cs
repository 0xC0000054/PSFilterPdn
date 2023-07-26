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

using System;
using System.Drawing;
using System.Drawing.Imaging;

#nullable enable

namespace PSFilterLoad.PSApi.Imaging.Internal
{
    internal sealed unsafe class DisplayPixelsSurfaceLock : Disposable, IDisplayPixelsSurfaceLock
    {
        private readonly ISurfaceLock surfaceLock;
        private readonly PixelFormat bitmapPixelFormat;

        public DisplayPixelsSurfaceLock(ISurfaceLock surfaceLock)
        {
            ArgumentNullException.ThrowIfNull(surfaceLock, nameof(surfaceLock));

            this.surfaceLock = surfaceLock;

            switch (surfaceLock.Format)
            {
                case SurfacePixelFormat.Bgra32:
                    bitmapPixelFormat = PixelFormat.Format32bppArgb;
                    break;
                case SurfacePixelFormat.Bgr24:
                    bitmapPixelFormat = PixelFormat.Format24bppRgb;
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported {nameof(SurfacePixelFormat)}: {surfaceLock.Format}.");
            }
        }

        public unsafe void* Buffer
        {
            get
            {
                VerifyNotDisposed();

                return surfaceLock.Buffer;
            }
        }

        public int BufferStride => surfaceLock.BufferStride;

        public int Width => surfaceLock.Width;

        public int Height => surfaceLock.Height;

        public SurfacePixelFormat Format => surfaceLock.Format;

        public Bitmap CreateAliasedBitmap()
        {
            VerifyNotDisposed();

            return new Bitmap(Width,
                              Height,
                              BufferStride,
                              bitmapPixelFormat,
                              (nint)Buffer);
        }

        public byte* GetPointPointerUnchecked(int x, int y)
        {
            VerifyNotDisposed();

            return surfaceLock.GetPointPointerUnchecked(x, y);
        }

        public byte* GetRowPointerUnchecked(int y)
        {
            VerifyNotDisposed();

            return surfaceLock.GetRowPointerUnchecked(y);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                surfaceLock.Dispose();
            }
        }
    }
}
