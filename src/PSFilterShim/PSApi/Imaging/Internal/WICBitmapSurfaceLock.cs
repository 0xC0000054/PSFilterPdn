/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2025 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using TerraFX.Interop.Windows;

namespace PSFilterLoad.PSApi.Imaging.Internal
{
    internal sealed unsafe class WICBitmapSurfaceLock<TPixel> : Disposable, ISurfaceLock where TPixel : unmanaged, IPixelFormatInfo
    {
        private readonly ComPtr<IWICBitmapLock> bitmapLock;
        private readonly byte* scan0;
        private readonly nuint pixelSizeInBytes;

        public WICBitmapSurfaceLock(ref ComPtr<IWICBitmapLock> bitmapLock)
        {
            uint width = 0;
            uint height = 0;
            uint bufferStride = 0;
            uint totalBufferSize = 0;
            byte* scan0 = null;

            HRESULT hr = bitmapLock.Get()->GetSize(&width, &height);
            WICException.ThrowIfFailed("IWICBitmapLock->GetSize failed.", hr);

            hr = bitmapLock.Get()->GetStride(&bufferStride);
            WICException.ThrowIfFailed("IWICBitmapLock->GetStride failed.", hr);

            hr = bitmapLock.Get()->GetDataPointer(&totalBufferSize, &scan0);
            WICException.ThrowIfFailed("IWICBitmapLock->GetDataPointer failed.", hr);

            this.scan0 = scan0;
            BufferStride = checked((int)bufferStride);
            Height = checked((int)height);
            Width = checked((int)width);
            Format = TPixel.Format;
            pixelSizeInBytes = (uint)TPixel.BytesPerPixel;

            this.bitmapLock.Swap(ref bitmapLock);
        }

        public void* Buffer
        {
            get
            {
                VerifyNotDisposed();

                return scan0;
            }
        }

        public int BufferStride { get; }

        public SurfacePixelFormat Format { get; }

        public int Height { get; }

        public int Width { get; }

        public byte* GetPointPointerUnchecked(int x, int y)
        {
            VerifyNotDisposed();

            return GetRowPointerUnchecked(y) + ((nuint)x * pixelSizeInBytes);
        }

        public byte* GetRowPointerUnchecked(int y)
        {
            VerifyNotDisposed();

            return scan0 + ((nuint)y * (nuint)BufferStride);
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
