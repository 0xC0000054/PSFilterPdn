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
using PaintDotNet.AppModel;
using PaintDotNet.Imaging;
using PaintDotNet.Rendering;
using System;

namespace PSFilterLoad.PSApi.Imaging.Internal
{
    internal sealed unsafe class PDNTransparencyCheckerboardSurface : TransparencyCheckerboardSurface
    {
        private readonly WICBitmapSurface<ColorPbgra32> bitmap;

        public PDNTransparencyCheckerboardSurface(IServiceProvider serviceProvider, IImagingFactory imagingFactory)
        {
            IVisualStylingService visualStylingService = serviceProvider.GetService<IVisualStylingService>() ?? throw new InvalidOperationException("Failed to get the visual styling service.");
            ICheckerboardVisualStyling checkerboardVisualStyling = visualStylingService.Checkerboard;

            SizeInt32 size = checkerboardVisualStyling.GetRepeatingBitmapSize(CheckerboardTileSize.Medium);

            Width = size.Width;
            Height = size.Height;
            bitmap = new WICBitmapSurface<ColorPbgra32>(Width, Height, imagingFactory);

            try
            {
                using (ISurfaceLock surfaceLock = bitmap.Lock(SurfaceLockMode.Write))
                {
                    RegionPtr<ColorPbgra32> region = new((ColorPbgra32*)surfaceLock.Buffer,
                                                         surfaceLock.Width,
                                                         surfaceLock.Height,
                                                         surfaceLock.BufferStride);

                    checkerboardVisualStyling.Render(region.Cast<ColorBgr32>(), CheckerboardTileSize.Medium);
                    PixelKernels.SetAlphaChannel(region.Cast<ColorBgra32>(), ColorAlpha8.Opaque);
                }
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }
        }

        private PDNTransparencyCheckerboardSurface(PDNTransparencyCheckerboardSurface cloneMe)
        {
            Width = cloneMe.Width;
            Height = cloneMe.Height;
            bitmap = cloneMe.bitmap.Clone();
        }

        public override int Width { get; }

        public override int Height { get; }

        public override TransparencyCheckerboardSurface Clone()
        {
            VerifyNotDisposed();

            return new PDNTransparencyCheckerboardSurface(this);
        }

        public override ISurfaceLock Lock(Rectangle bounds, SurfaceLockMode mode)
        {
            VerifyNotDisposed();

            if (mode != SurfaceLockMode.Read)
            {
                ExceptionUtil.ThrowArgumentException("The transparency checkerboard is read only.", nameof(mode));
            }

            return bitmap.Lock(bounds, mode);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                bitmap.Dispose();
            }
        }
    }
}
