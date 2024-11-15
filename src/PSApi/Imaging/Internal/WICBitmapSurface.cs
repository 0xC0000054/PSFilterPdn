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
    internal sealed unsafe class WICBitmapSurface<TPixel> : ImageSurface where TPixel : unmanaged, INaturalPixelInfo
    {
        private readonly IBitmap<TPixel> bitmap;
        private readonly IImagingFactory imagingFactory;

        public WICBitmapSurface(int width, int height, IImagingFactory imagingFactory)
        {
            ArgumentNullException.ThrowIfNull(imagingFactory, nameof(imagingFactory));

            (int channelCount, int bitsPerChannel, SurfacePixelFormat format) = GetFormatInfo(typeof(TPixel));

            ChannelCount = channelCount;
            BitsPerChannel = bitsPerChannel;
            Format = format;
            Width = width;
            Height = height;

            this.imagingFactory = imagingFactory;

            bitmap = imagingFactory.CreateBitmap<TPixel>(width, height, BitmapAllocationOptions.Contiguous);
        }

        private WICBitmapSurface(WICBitmapSurface<TPixel> original, int newWidth, int newHeight)
        {
            ArgumentNullException.ThrowIfNull(original, nameof(original));

            Width = newWidth;
            Height = newHeight;
            ChannelCount = original.ChannelCount;
            BitsPerChannel = original.BitsPerChannel;
            Format = original.Format;

            imagingFactory = original.imagingFactory;

            bitmap = imagingFactory.CreateBitmap<TPixel>(newWidth, newHeight);

            SizeInt32 dstSize = new(newWidth, newHeight);
            BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQualityCubic;

            using (IBitmapSource<TPixel> scaled = imagingFactory.CreateBitmapScaler(original.bitmap, dstSize, interpolationMode))
            {
                using (IBitmapLock<TPixel> destination = bitmap.Lock(BitmapLockOptions.Write))
                {
                    scaled.CopyPixels(destination.Buffer, destination.BufferStride, destination.BufferSize, null);
                }
            }
        }

        private WICBitmapSurface(WICBitmapSurface<TPixel> cloneMe)
        {
            imagingFactory = cloneMe.imagingFactory;

            ChannelCount = cloneMe.ChannelCount;
            BitsPerChannel = cloneMe.BitsPerChannel;
            Format = cloneMe.Format;
            Width = cloneMe.Width;
            Height = cloneMe.Height;

            bitmap = imagingFactory.CreateBitmap<TPixel>(Width, Height);

            using (IBitmapLock<TPixel> source = cloneMe.bitmap.Lock(BitmapLockOptions.Read))
            using (IBitmapLock<TPixel> destination = bitmap.Lock(BitmapLockOptions.Write))
            {
                source.AsRegionPtr().CopyTo(destination.AsRegionPtr());
            }
        }

        public override int Width { get; }

        public override int Height { get; }

        public override int ChannelCount { get; }

        public override int BitsPerChannel { get; }

        public override SurfacePixelFormat Format { get; }

        public override bool SupportsTransparency => default(TPixel).SupportsTransparency;

        public override WICBitmapSurface<TPixel> Clone()
        {
            VerifyNotDisposed();

            return new(this);
        }

        public override WICBitmapSurface<TPixel> CreateScaledSurface(int newWidth, int newHeight)
        {
            VerifyNotDisposed();

            return new WICBitmapSurface<TPixel>(this, newWidth, newHeight);
        }

        public override ISurfaceLock Lock(Rectangle bounds, SurfaceLockMode mode)
        {
            VerifyNotDisposed();

            RectInt32 rectInt32Bounds = new(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            BitmapLockOptions bitmapLockOptions;

            switch (mode)
            {
                case SurfaceLockMode.Read:
                    bitmapLockOptions = BitmapLockOptions.Read;
                    break;
                case SurfaceLockMode.Write:
                    bitmapLockOptions = BitmapLockOptions.Write;
                    break;
                case SurfaceLockMode.ReadWrite:
                    bitmapLockOptions = BitmapLockOptions.ReadWrite;
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported {nameof(SurfaceLockMode)}: {mode}.");
            }

            IBitmapLock<TPixel> bitmapLock = bitmap.Lock(rectInt32Bounds, bitmapLockOptions);

            return new WICBitmapSurfaceLock<TPixel>(bitmapLock, Format);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                bitmap.Dispose();
                // The WIC imaging factory is owned by external code.
            }
        }

        private static (int, int, SurfacePixelFormat) GetFormatInfo(Type pixelType)
        {
            int channelCount;
            int bitsPerChannel;
            SurfacePixelFormat format;

            if (pixelType == typeof(ColorAlpha8))
            {
                channelCount = 1;
                bitsPerChannel = 8;
                format = SurfacePixelFormat.Gray8;
            }
            else if (pixelType == typeof(ColorBgra32))
            {
                channelCount = 4;
                bitsPerChannel = 8;
                format = SurfacePixelFormat.Bgra32;
            }
            else if (pixelType == typeof(ColorPbgra32))
            {
                channelCount = 4;
                bitsPerChannel = 8;
                format = SurfacePixelFormat.Pbgra32;
            }
            else
            {
                throw new NotSupportedException($"Unsupported pixel type: {pixelType}");
            }

            return (channelCount, bitsPerChannel, format);
        }
    }
}
