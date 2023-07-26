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

#nullable enable

namespace PSFilterLoad.PSApi.Imaging.Internal
{
    internal sealed unsafe class ShimSurfaceBgr24Lock : Disposable, ISurfaceLock
    {
        public ShimSurfaceBgr24Lock(void* buffer, int bufferStride, int width, int height)
        {
            Buffer = buffer;
            BufferStride = bufferStride;
            Width = width;
            Height = height;
        }

        public void* Buffer { get; }

        public int BufferStride { get; }

        public SurfacePixelFormat Format => SurfacePixelFormat.Bgr24;

        public int Height { get; }

        public int Width { get; }

        public byte* GetPointPointerUnchecked(int x, int y)
            => GetRowPointerUnchecked(y) + ((long)x * 3);

        public byte* GetRowPointerUnchecked(int y)
            => (byte*)Buffer + ((long)y * BufferStride);

        protected override void Dispose(bool disposing)
        {
        }
    }
}
