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
    internal sealed unsafe class ShimSurfaceBgra32Lock : Disposable, ISurfaceLock
    {
        public ShimSurfaceBgra32Lock(void* buffer,
                                     int bufferStride,
                                     int width,
                                     int height)
        {
            Buffer = buffer;
            BufferStride = bufferStride;
            Width = width;
            Height = height;
        }

        public void* Buffer { get; }

        public int BufferStride { get; }

        public int Width { get; }

        public int Height { get; }

        public SurfacePixelFormat Format => SurfacePixelFormat.Bgra32;

        public byte* GetPointPointerUnchecked(int x, int y)
        {
            return GetRowPointerUnchecked(y) + ((long)x * 4);
        }

        public byte* GetRowPointerUnchecked(int y)
        {
            return (byte*)Buffer + ((long)y * BufferStride);
        }

        protected override void Dispose(bool disposing)
        {
        }
    }
}
