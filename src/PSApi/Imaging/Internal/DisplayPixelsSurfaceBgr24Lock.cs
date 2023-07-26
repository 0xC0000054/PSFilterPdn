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
    internal sealed unsafe class DisplayPixelsSurfaceBgr24Lock : Disposable, IDisplayPixelsSurfaceLock
    {
        public DisplayPixelsSurfaceBgr24Lock(void* buffer, int bufferStride, int width, int height)
        {
            Buffer = buffer;
            BufferStride = bufferStride;
            Height = height;
            Width = width;
        }

        public void* Buffer { get; }

        public int BufferStride { get; }

        public SurfacePixelFormat Format => SurfacePixelFormat.Bgr24;

        public int Height { get; }

        public int Width { get; }

        public Bitmap CreateAliasedBitmap()
        {
            return new Bitmap(Width,
                              Height,
                              BufferStride,
                              System.Drawing.Imaging.PixelFormat.Format24bppRgb,
                              (nint)Buffer);
        }

        public byte* GetPointPointerUnchecked(int x, int y)
            => GetRowPointerUnchecked(y) + (nuint)((long)x * 3);

        public byte* GetRowPointerUnchecked(int y)
            => (byte*)Buffer + (nuint)((long)y * BufferStride);

        protected override void Dispose(bool disposing)
        {
        }
    }
}
