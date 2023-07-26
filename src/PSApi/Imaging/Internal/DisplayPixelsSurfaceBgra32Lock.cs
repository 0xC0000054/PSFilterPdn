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

namespace PSFilterLoad.PSApi.Imaging.Internal
{
    internal sealed unsafe class DisplayPixelsSurfaceBgra32Lock : Disposable, IDisplayPixelsSurfaceLock
    {
        private readonly ISurfaceLock surfaceLock;

        public DisplayPixelsSurfaceBgra32Lock(ISurfaceLock surfaceLock)
        {
            this.surfaceLock = surfaceLock;
        }

        public void* Buffer
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

        public SurfacePixelFormat Format => SurfacePixelFormat.Bgra32;

        public Bitmap CreateAliasedBitmap()
        {
            return new Bitmap(Width,
                              Height,
                              BufferStride,
                              System.Drawing.Imaging.PixelFormat.Format32bppArgb,
                              (nint)Buffer);
        }

        public byte* GetPointPointerUnchecked(int x, int y)
        {
            return GetRowPointerUnchecked(y) + (nuint)((nint)x * 4);
        }

        public byte* GetRowPointerUnchecked(int y)
        {
            return (byte*)Buffer + (nuint)((nint)y * BufferStride);
        }

        protected override void Dispose(bool disposing)
        {
        }
    }
}
