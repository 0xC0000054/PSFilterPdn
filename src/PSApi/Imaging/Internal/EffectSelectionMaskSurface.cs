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

using PaintDotNet.Effects;
using PaintDotNet.Imaging;
using PaintDotNet.Rendering;
using PaintDotNet;
using System;

namespace PSFilterLoad.PSApi.Imaging.Internal
{
    internal sealed unsafe class EffectSelectionMaskSurface : MaskSurface
    {
        private readonly IEffectInputBitmap<ColorAlpha8> effectInputBitmap;
        private readonly IImagingFactory imagingFactory;

        public EffectSelectionMaskSurface(IEffectInputBitmap<ColorAlpha8> effectInputBitmap, IImagingFactory imagingFactory)
        {
            ArgumentNullException.ThrowIfNull(effectInputBitmap, nameof(effectInputBitmap));
            ArgumentNullException.ThrowIfNull(imagingFactory, nameof(imagingFactory));

            this.effectInputBitmap = effectInputBitmap;
            this.imagingFactory = imagingFactory;

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

            RectInt32 rectInt32Bounds = new(bounds.X, bounds.Y, bounds.Width, bounds.Height);

            IBitmapLock<ColorAlpha8> bitmapLock = effectInputBitmap.Lock(rectInt32Bounds);

            return new WICBitmapSurfaceLock<ColorAlpha8>(bitmapLock, SurfacePixelFormat.Gray8);
        }

        protected override void Dispose(bool disposing)
        {
            // Not required as Paint.NET owns the image.
        }
    }
}
