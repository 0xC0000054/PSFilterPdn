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

using TerraFX.Interop.Windows;
using System;

using static TerraFX.Interop.Windows.GUID;
using static TerraFX.Interop.Windows.Pointers;
using static TerraFX.Interop.Windows.WICBitmapCreateCacheOption;
using static TerraFX.Interop.Windows.WICBitmapLockFlags;

namespace PSFilterLoad.PSApi.Imaging.Internal
{
    internal sealed unsafe class WICBitmapSurface<TPixel> : ImageSurface where TPixel : unmanaged, IPixelFormatInfo
    {
        private readonly ComPtr<IWICBitmap> bitmap;
        private readonly IWICFactory factory;

        public WICBitmapSurface(int width, int height, IWICFactory factory)
        {
            ArgumentNullException.ThrowIfNull(factory, nameof(factory));
            ThrowIfNotPositive(width, nameof(width));
            ThrowIfNotPositive(height, nameof(height));

            Width = width;
            Height = height;
            ChannelCount = TPixel.ChannelCount;
            BitsPerChannel = TPixel.BitsPerChannel;
            Format = TPixel.Format;
            SupportsTransparency = TPixel.SupportsTransparency;
            this.factory = factory;

            Guid wicPixelFormat = GetWICPixelFormat(Format);

            fixed (IWICBitmap** ppBitmap = bitmap)
            {
                HRESULT hr = factory.Get()->CreateBitmap((uint)width,
                                                         (uint)height,
                                                         &wicPixelFormat,
                                                         WICBitmapCacheOnLoad,
                                                         ppBitmap);
                WICException.ThrowIfFailed("Failed to create the WIC Bitmap.", hr);
            }
            this.factory = factory;
        }

        private WICBitmapSurface(WICBitmapSurface<TPixel> original, int newWidth, int newHeight)
        {
            ThrowIfNotPositive(newWidth, nameof(newWidth));
            ThrowIfNotPositive(newHeight, nameof(newHeight));

            factory = original.factory;
            Width = newWidth;
            Height = newHeight;
            ChannelCount = original.ChannelCount;
            BitsPerChannel = original.BitsPerChannel;
            Format = original.Format;
            SupportsTransparency = original.SupportsTransparency;

            Guid wicPixelFormat = GetWICPixelFormat(Format);

            HRESULT hr;
            fixed (IWICBitmap** ppBitmap = bitmap)
            {
                hr = factory.Get()->CreateBitmap((uint)newWidth,
                                                 (uint)newHeight,
                                                 &wicPixelFormat,
                                                 WICBitmapCacheOnLoad,
                                                 ppBitmap);
            }
            WICException.ThrowIfFailed("Failed to create the WIC Bitmap.", hr);

            try
            {
                using (ComPtr<IWICBitmapScaler> bitmapScaler = default)
                {
                    hr = factory.Get()->CreateBitmapScaler(bitmapScaler.GetAddressOf());
                    WICException.ThrowIfFailed("Failed to create the bitmap scaler.", hr);

                    hr = bitmapScaler.Get()->Initialize(__cast(original.bitmap),
                                                        (uint)newWidth,
                                                        (uint)newHeight,
                                                        WICBitmapInterpolationMode.WICBitmapInterpolationModeHighQualityCubic);
                    WICException.ThrowIfFailed("Failed to initialize the bitmap scaler.", hr);

                    using (ComPtr<IWICBitmapLock> bitmapLock = default)
                    {
                        hr = bitmap.Get()->Lock(null, (uint)WICBitmapLockWrite, bitmapLock.GetAddressOf());
                        WICException.ThrowIfFailed("Failed to lock the bitmap.", hr);

                        uint bufferStride = 0;
                        uint totalBufferSize = 0;
                        byte* scan0 = null;

                        hr = bitmapLock.Get()->GetStride(&bufferStride);
                        WICException.ThrowIfFailed("IWICBitmapLock->GetStride failed.", hr);

                        hr = bitmapLock.Get()->GetDataPointer(&totalBufferSize, &scan0);
                        WICException.ThrowIfFailed("IWICBitmapLock->GetDataPointer failed.", hr);

                        hr = bitmapScaler.Get()->CopyPixels(null, bufferStride, totalBufferSize, scan0);
                        WICException.ThrowIfFailed("IWICBitmapScaler->CopyPixels failed.", hr);
                    }
                }
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }
        }

        private WICBitmapSurface(WICBitmapSurface<TPixel> original)
        {
            factory = original.factory;
            Width = original.Width;
            Height = original.Height;
            ChannelCount = original.ChannelCount;
            BitsPerChannel = original.BitsPerChannel;
            Format = original.Format;
            SupportsTransparency = original.SupportsTransparency;

            Guid wicPixelFormat = GetWICPixelFormat(Format);

            HRESULT hr;
            fixed (IWICBitmap** ppBitmap = bitmap)
            {
                hr = factory.Get()->CreateBitmap((uint)original.Width,
                                                 (uint)original.Height,
                                                 &wicPixelFormat,
                                                 WICBitmapCacheOnLoad,
                                                 ppBitmap);
            }
            WICException.ThrowIfFailed("Failed to create the WIC Bitmap.", hr);

            try
            {
                using (ComPtr<IWICBitmapLock> bitmapLock = default)
                {
                    hr = bitmap.Get()->Lock(null, (uint)WICBitmapLockWrite, bitmapLock.GetAddressOf());
                    WICException.ThrowIfFailed("Failed to lock the bitmap.", hr);

                    uint bufferStride = 0;
                    uint totalBufferSize = 0;
                    byte* scan0 = null;

                    hr = bitmapLock.Get()->GetStride(&bufferStride);
                    WICException.ThrowIfFailed("IWICBitmapLock->GetStride failed.", hr);

                    hr = bitmapLock.Get()->GetDataPointer(&totalBufferSize, &scan0);
                    WICException.ThrowIfFailed("IWICBitmapLock->GetDataPointer failed.", hr);

                    hr = original.bitmap.Get()->CopyPixels(null, bufferStride, totalBufferSize, scan0);
                    WICException.ThrowIfFailed("IWICBitmapScaler->CopyPixels failed.", hr);
                }
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }
        }


        public override int Width { get; }

        public override int Height { get; }

        public override int ChannelCount { get; }

        public override int BitsPerChannel { get; }

        public override SurfacePixelFormat Format { get; }

        public override bool SupportsTransparency { get; }

        public override WICBitmapSurface<TPixel> Clone()
        {
            VerifyNotDisposed();

            return new WICBitmapSurface<TPixel>(this);
        }

        public override WICBitmapSurface<TPixel> CreateScaledSurface(int newWidth, int newHeight)
        {
            VerifyNotDisposed();

            return new WICBitmapSurface<TPixel>(this, newWidth, newHeight);
        }

        public override ISurfaceLock Lock(SurfaceLockMode mode)
        {
            VerifyNotDisposed();

            return Lock(null, mode);
        }

        public override ISurfaceLock Lock(Rectangle bounds, SurfaceLockMode mode)
        {
            VerifyNotDisposed();

            WICRect rect = new()
            {
                X = bounds.X,
                Y = bounds.Y,
                Width = bounds.Width,
                Height = bounds.Height
            };

            return Lock(&rect, mode);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                bitmap.Dispose();
            }
        }

        private static Guid GetWICPixelFormat(SurfacePixelFormat format)
        {
            Guid wicPixelFormat;

            switch (format)
            {
                case SurfacePixelFormat.Bgra32:
                    wicPixelFormat = GUID_WICPixelFormat32bppBGRA;
                    break;
                case SurfacePixelFormat.Gray8:
                    wicPixelFormat = GUID_WICPixelFormat8bppGray;
                    break;
                case SurfacePixelFormat.Pbgra32:
                    wicPixelFormat = GUID_WICPixelFormat32bppPBGRA;
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported {nameof(SurfacePixelFormat)} value: {format}.");
            }

            return wicPixelFormat;
        }

        private static void ThrowIfNotPositive(int value, string paramName)
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(paramName, "Must be positive.");
            }
        }

        private ISurfaceLock Lock(WICRect* rect, SurfaceLockMode mode)
        {
            ISurfaceLock surfaceLock;

            uint nativeLockMode;

            switch (mode)
            {
                case SurfaceLockMode.Read:
                    nativeLockMode = (uint)WICBitmapLockRead;
                    break;
                case SurfaceLockMode.Write:
                    nativeLockMode = (uint)WICBitmapLockWrite;
                    break;
                case SurfaceLockMode.ReadWrite:
                    nativeLockMode = (uint)(WICBitmapLockRead | WICBitmapLockWrite);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported {nameof(SurfaceLockMode)} value: {mode}.");
            }

            ComPtr<IWICBitmapLock> bitmapLock = default;

            try
            {
                HRESULT hr = bitmap.Get()->Lock(rect, nativeLockMode, bitmapLock.GetAddressOf());
                WICException.ThrowIfFailed("Failed to lock the bitmap.", hr);

                surfaceLock = new WICBitmapSurfaceLock<TPixel>(ref bitmapLock);
            }
            finally
            {
                bitmapLock.Dispose();
            }

            return surfaceLock;
        }
    }
}
