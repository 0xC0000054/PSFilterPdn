/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2024 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PaintDotNet.Imaging;
using PaintDotNet.Rendering;
using System;

namespace PSFilterLoad.PSApi.Imaging.Internal
{
    internal sealed unsafe class WICBitmapSurfaceLock<TPixel> : Disposable, ISurfaceLock where TPixel : unmanaged, INaturalPixelInfo
    {
        private static readonly int BytesPerPixel = default(TPixel).BytesPerPixel;

        private readonly IBitmapLock<TPixel> bitmapLock;

        public WICBitmapSurfaceLock(IBitmapLock<TPixel> bitmapLock, SurfacePixelFormat format)
        {
            this.bitmapLock = bitmapLock ?? throw new ArgumentNullException(nameof(bitmapLock));

            SizeInt32 size = bitmapLock.Size;

            Width = size.Width;
            Height = size.Height;
            Format = format;
        }

        public void* Buffer
        {
            get
            {
                VerifyNotDisposed();

                return bitmapLock.Buffer;
            }
        }

        public int BufferStride => bitmapLock.BufferStride;

        public int Width { get; }

        public int Height { get; }

        public SurfacePixelFormat Format { get; }


        public byte* GetPointPointerUnchecked(int x, int y)
        {
            return GetRowPointerUnchecked(y) + (nuint)((nint)x * BytesPerPixel);
        }

        public byte* GetRowPointerUnchecked(int y)
        {
            return (byte*)bitmapLock.Buffer + (nuint)((nint)y * bitmapLock.BufferStride);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                bitmapLock.Dispose();
            }
        }
    }
}
