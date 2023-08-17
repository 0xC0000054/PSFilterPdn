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

using PaintDotNet.Effects;
using PaintDotNet.Imaging;
using PaintDotNet.Rendering;
using PaintDotNet;
using System;

#nullable enable

namespace PSFilterLoad.PSApi.Imaging.Internal
{
    internal sealed unsafe class EffectSelectionMaskSurface : MaskSurface
    {
        private readonly IEffectInputBitmap<ColorAlpha8> effectInputBitmap;
        private readonly IImagingFactory imagingFactory;

        public EffectSelectionMaskSurface(IEffectInputBitmap<ColorAlpha8> effectInputBitmap, IServiceProvider serviceProvider)
        {
            ArgumentNullException.ThrowIfNull(effectInputBitmap, nameof(effectInputBitmap));
            ArgumentNullException.ThrowIfNull(serviceProvider, nameof(serviceProvider));

            this.effectInputBitmap = effectInputBitmap;
            imagingFactory = serviceProvider.GetService<IImagingFactory>() ?? throw new InvalidOperationException("Failed to get the WIC factory.");

            SizeInt32 size = effectInputBitmap.Size;

            Width = size.Width;
            Height = size.Height;
        }

        private EffectSelectionMaskSurface(EffectSelectionMaskSurface cloneMe)
        {
            effectInputBitmap = cloneMe.effectInputBitmap;
            imagingFactory = cloneMe.imagingFactory;

            SizeInt32 size = effectInputBitmap.Size;

            Width = size.Width;
            Height = size.Height;
        }

        public override int Width { get; }

        public override int Height { get; }

        public override bool IsReadOnly => true;

        public override ISurface<MaskSurface> Clone()
        {
            VerifyNotDisposed();

            return new EffectSelectionMaskSurface(this);
        }

        public override ISurface<MaskSurface> CreateScaledSurface(int newWidth, int newHeight)
        {
            VerifyNotDisposed();

            PDNMaskSurface scaledSurface = new(newWidth, newHeight, imagingFactory);

            using (IBitmapSource<ColorAlpha8> bitmapScaler = imagingFactory.CreateBitmapScaler<ColorAlpha8>(effectInputBitmap,
                                                                                                            newWidth,
                                                                                                            newHeight,
                                                                                                            BitmapInterpolationMode.HighQualityCubic))
            using (ISurfaceLock bitmapLock = scaledSurface.Lock(SurfaceLockMode.Write))
            {
                RegionPtr<ColorAlpha8> dst = new((ColorAlpha8*)bitmapLock.Buffer,
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
                ExceptionUtil.ThrowArgumentException("Cannot lock the effect selection mask surface for writing.", nameof(mode));
            }

            RectInt32 rectInt32Bounds = bounds;

            IBitmapLock<ColorAlpha8> bitmapLock = effectInputBitmap.Lock(rectInt32Bounds);

            return new WICBitmapSurfaceLock<ColorAlpha8>(bitmapLock, SurfacePixelFormat.Gray8);
        }

        protected override void Dispose(bool disposing)
        {
            // Not required as Paint.NET owns the image.
        }
    }
}
