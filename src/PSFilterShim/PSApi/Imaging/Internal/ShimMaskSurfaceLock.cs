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
    internal sealed unsafe class ShimMaskSurfaceLock : Disposable, ISurfaceLock
    {
        public ShimMaskSurfaceLock(void* buffer, int bufferStride, int width, int height)
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

        public SurfacePixelFormat Format => SurfacePixelFormat.Gray8;

        public byte* GetPointPointerUnchecked(int x, int y)
        {
            return GetRowPointerUnchecked(y) + x;
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
