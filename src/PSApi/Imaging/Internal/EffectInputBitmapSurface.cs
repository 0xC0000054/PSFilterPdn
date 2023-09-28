/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2023 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.Imaging;
using PaintDotNet.Rendering;
using System;

namespace PSFilterLoad.PSApi.Imaging.Internal
{
    internal sealed unsafe class EffectInputBitmapSurface : ImageSurface, ISurfaceHasTransparency
    {
        private readonly IEffectInputBitmap<ColorBgra32> effectInputBitmap;
        private readonly IImagingFactory imagingFactory;
        private readonly Lazy<bool> imageHasTransparency;

        public EffectInputBitmapSurface(IEffectInputBitmap<ColorBgra32> effectInputBitmap, IServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(effectInputBitmap, nameof(effectInputBitmap));
            ArgumentNullException.ThrowIfNull(serviceProvider, nameof(serviceProvider));

            this.effectInputBitmap = effectInputBitmap;
            imagingFactory = serviceProvider.GetService<IImagingFactory>() ?? throw new InvalidOperationException("Failed to get the WIC factory.");
            imageHasTransparency = new Lazy<bool>(ImageHasTransparency);

            SizeInt32 size = effectInputBitmap.Size;

            Width = size.Width;
            Height = size.Height;
        }

        public EffectInputBitmapSurface(IEffectInputBitmap<ColorBgra32> effectInputBitmap, IImagingFactory imagingFactory)
        {
            this.effectInputBitmap = effectInputBitmap ?? throw new ArgumentNullException(nameof(effectInputBitmap));
            this.imagingFactory = imagingFactory ?? throw new ArgumentNullException(nameof(imagingFactory));
            imageHasTransparency = new Lazy<bool>(ImageHasTransparency);

            SizeInt32 size = effectInputBitmap.Size;

            Width = size.Width;
            Height = size.Height;
        }

        private EffectInputBitmapSurface(EffectInputBitmapSurface cloneMe)
        {
            effectInputBitmap = cloneMe.effectInputBitmap;
            imagingFactory = cloneMe.imagingFactory;
            imageHasTransparency = cloneMe.imageHasTransparency;

            SizeInt32 size = effectInputBitmap.Size;

            Width = size.Width;
            Height = size.Height;
        }

        public override int Width { get; }

        public override int Height { get; }

        public override int ChannelCount => 4;

        public override int BitsPerChannel => 8;

        public override SurfacePixelFormat Format => SurfacePixelFormat.Bgra32;

        public override bool SupportsTransparency => true;

        public override bool IsReadOnly => true;

        public override ISurface<ImageSurface> Clone()
        {
            VerifyNotDisposed();

            return new EffectInputBitmapSurface(this);
        }

        public override ISurface<ImageSurface> CreateScaledSurface(int newWidth, int newHeight)
        {
            VerifyNotDisposed();

            WICBitmapSurface<ColorBgra32> scaledSurface = new(newWidth, newHeight, imagingFactory);

            using (IBitmapSource<ColorBgra32> bitmapScaler = imagingFactory.CreateBitmapScaler<ColorBgra32>(effectInputBitmap,
                                                                                                            newWidth,
                                                                                                            newHeight,
                                                                                                            BitmapInterpolationMode.HighQualityCubic))
            using (ISurfaceLock bitmapLock = scaledSurface.Lock(SurfaceLockMode.Write))
            {
                RegionPtr<ColorBgra32> dst = new((ColorBgra32*)bitmapLock.Buffer,
                                                 bitmapLock.Width,
                                                 bitmapLock.Height,
                                                 bitmapLock.BufferStride);

                bitmapScaler.CopyPixels(dst);
            }

            return scaledSurface;
        }

        public override ISurfaceLock Lock(Rectangle bounds, SurfaceLockMode mode)
        {
            VerifyNotDisposed();

            if (mode != SurfaceLockMode.Read)
            {
                ExceptionUtil.ThrowArgumentException("Cannot lock the effect input surface for writing.", nameof(mode));
            }

            RectInt32 rectInt32Bounds = new(bounds.X, bounds.Y, bounds.Width, bounds.Height);

            IBitmapLock<ColorBgra32> bitmapLock = effectInputBitmap.Lock(rectInt32Bounds);

            return new WICBitmapSurfaceLock<ColorBgra32>(bitmapLock, Format);
        }

        bool ISurfaceHasTransparency.HasTransparency() => imageHasTransparency.Value;

        protected override void Dispose(bool disposing)
        {
            // Not required as Paint.NET owns the image.
        }

        private bool ImageHasTransparency()
        {
            RectInt32 bounds = new(0, 0, Width, Height);

            using (IBitmapLock<ColorBgra32> bitmapLock = effectInputBitmap.Lock(bounds))
            {
                RegionPtr<ColorBgra32> region = bitmapLock.AsRegionPtr();

                foreach (RegionRowPtr<ColorBgra32> row in region.Rows)
                {
                    ColorBgra32* ptr = row.Ptr;
                    ColorBgra32* ptrEnd = row.EndPtr;

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

            return false;
        }
    }
}
