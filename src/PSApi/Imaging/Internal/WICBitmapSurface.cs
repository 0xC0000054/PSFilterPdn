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

using PaintDotNet;
using PaintDotNet.Imaging;
using PaintDotNet.Rendering;
using System;
using System.Drawing;

#nullable enable

namespace PSFilterLoad.PSApi.Imaging.Internal
{
    internal sealed unsafe class WICBitmapSurface<TPixel> : ImageSurface, IWICBitmapSurface where TPixel : unmanaged, INaturalPixelInfo
    {
        private readonly IBitmap<TPixel> bitmap;
        private readonly IImagingFactory imagingFactory;

        public WICBitmapSurface(IBitmapSource<TPixel> bitmapSource, IServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(bitmapSource, nameof(bitmapSource));
            ArgumentNullException.ThrowIfNull(serviceProvider, nameof(serviceProvider));

            (int channelCount, int bitsPerChannel, SurfacePixelFormat format) = GetFormatInfo(typeof(TPixel));
            SizeInt32 size = bitmapSource.Size;

            ChannelCount = channelCount;
            BitsPerChannel = bitsPerChannel;
            Format = format;
            Width = size.Width;
            Height = size.Height;

            imagingFactory = serviceProvider.GetService<IImagingFactory>() ?? throw new InvalidOperationException("Failed to get the WIC factory.");

            bitmap = bitmapSource.ToBitmap();
        }

        public WICBitmapSurface(IBitmapSource<TPixel> bitmapSource,
                                IImagingFactory imagingFactory)
        {
            ArgumentNullException.ThrowIfNull(bitmapSource, nameof(bitmapSource));
            ArgumentNullException.ThrowIfNull(imagingFactory, nameof(imagingFactory));

            (int channelCount, int bitsPerChannel, SurfacePixelFormat format) = GetFormatInfo(typeof(TPixel));
            SizeInt32 size = bitmapSource.Size;

            ChannelCount = channelCount;
            BitsPerChannel = bitsPerChannel;
            Format = format;
            Width = size.Width;
            Height = size.Height;

            this.imagingFactory = imagingFactory;

            bitmap = bitmapSource.ToBitmap();
        }

        public WICBitmapSurface(int width, int height, IImagingFactory imagingFactory, bool takeOwnershipOfImagingFactory = false)
        {
            ArgumentNullException.ThrowIfNull(imagingFactory, nameof(imagingFactory));

            (int channelCount, int bitsPerChannel, SurfacePixelFormat format) = GetFormatInfo(typeof(TPixel));

            ChannelCount = channelCount;
            BitsPerChannel = bitsPerChannel;
            Format = format;
            Width = width;
            Height = height;

            this.imagingFactory = imagingFactory;

            try
            {
                bitmap = imagingFactory.CreateBitmap<TPixel>(width, height);
            }
            catch (Exception)
            {
                imagingFactory.Dispose();
                throw;
            }
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

        IImagingFactory IWICBitmapSurface.ImagingFactory => imagingFactory;

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

        public override bool HasTransparency()
        {
            VerifyNotDisposed();

            if (Format == SurfacePixelFormat.Bgra32)
            {
                using (IBitmapLock<TPixel> bitmapLock = bitmap.Lock(BitmapLockOptions.Read))
                {
                    int width = Width;
                    int height = Height;
                    byte* scan0 = (byte*)bitmapLock.Buffer;
                    int stride = bitmapLock.BufferStride;

                    for (int y = 0; y < height; y++)
                    {
                        ColorBgra32* ptr = (ColorBgra32*)(scan0 + (y * stride));
                        ColorBgra32* ptrEnd = ptr + width;

                        while (ptr < ptrEnd)
                        {
                            if (ptr->A < 255)
                            {
                                return true;
                            }

                            ptr++;
                        }
                    }
                }
            }

            return false;
        }

        public override ISurfaceLock Lock(Rectangle bounds, SurfaceLockMode mode)
        {
            VerifyNotDisposed();

            RectInt32 rectInt32Bounds = bounds;
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
            else
            {
                throw new NotSupportedException($"Unsupported pixel type: {pixelType}");
            }

            return (channelCount, bitsPerChannel, format);
        }
    }
}
