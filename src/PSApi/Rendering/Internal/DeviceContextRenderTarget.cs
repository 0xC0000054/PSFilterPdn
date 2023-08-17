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

using PaintDotNet.Direct2D1;
using PaintDotNet.Rendering;
using PSFilterLoad.PSApi.Imaging;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using System;

#nullable enable

namespace PSFilterLoad.PSApi.Rendering.Internal
{
    internal sealed unsafe class DeviceContextRenderTarget : Disposable, IDeviceContextRenderTarget
    {
        private readonly IDCDeviceContext deviceContext;

        public DeviceContextRenderTarget(IDirect2DFactory factory, bool maybeHasTransparency)
        {
            ArgumentNullException.ThrowIfNull(factory, nameof(factory));

            AlphaMode alphaMode = maybeHasTransparency ? AlphaMode.Premultiplied : AlphaMode.Ignore;

            DeviceContextProperties properties = new()
            {
                PixelFormat = new DevicePixelFormat(PaintDotNet.Dxgi.DxgiFormat.B8G8R8A8_UNorm, alphaMode)
            };

            deviceContext = factory.CreateDCDeviceContext(properties);
        }

        public D2D1_PRIMITIVE_BLEND PrimitiveBlend
        {
            get
            {
                VerifyNotDisposed();

                return (D2D1_PRIMITIVE_BLEND)deviceContext.PrimitiveBlend;
            }
            set
            {
                VerifyNotDisposed();

                deviceContext.PrimitiveBlend = (PrimitiveBlend)value;
            }
        }

        public bool SupportsTransparency
        {
            get
            {
                VerifyNotDisposed();

                return deviceContext.PixelFormat.AlphaMode == AlphaMode.Premultiplied;
            }
        }

        public void BeginDraw()
        {
            VerifyNotDisposed();

            try
            {
                deviceContext.BeginDraw();
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Direct2DException($"{nameof(BeginDraw)} failed", ex);
            }
        }

        public void BindToDeviceContext(HDC deviceContextHandle, RECT bounds)
        {
            VerifyNotDisposed();

            try
            {
                RectInt32 subRect = new(bounds.left,
                                        bounds.top,
                                        bounds.right - bounds.left,
                                        bounds.bottom - bounds.top);
                deviceContext.BindDC(deviceContextHandle, subRect);
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Direct2DException($"{nameof(BindToDeviceContext)} failed", ex);
            }
        }

        public IDeviceBitmap CreateBitmap(DisplayPixelsSurface surface, bool ignoreAlpha)
        {
            VerifyNotDisposed();

            IDeviceBitmap deviceBitmap;
            try
            {
                SizeInt32 size = new(surface.Width, surface.Height);
                AlphaMode alphaMode = ignoreAlpha ? AlphaMode.Ignore : AlphaMode.Premultiplied;

                using (ISurfaceLock surfaceLock = surface.Lock(SurfaceLockMode.Read))
                {
                    PaintDotNet.Direct2D1.IDeviceBitmap bitmap = deviceContext.CreateBitmap(size,
                                                                                            surfaceLock.Buffer,
                                                                                            surfaceLock.BufferStride,
                                                                                            PaintDotNet.Dxgi.DxgiFormat.B8G8R8A8_UNorm,
                                                                                            alphaMode);
                    deviceBitmap = new PDNDeviceBitmap(bitmap);
                }
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Direct2DException($"{nameof(CreateBitmap)} failed", ex);
            }

            return deviceBitmap;
        }

        public IDeviceBitmapBrush CreateBitmapBrush(TransparencyCheckerboardSurface surface)
        {
            VerifyNotDisposed();

            IDeviceBitmapBrush brush;
            try
            {
                SizeInt32 size = new(surface.Width, surface.Height);

                using (ISurfaceLock surfaceLock = surface.Lock(SurfaceLockMode.Read))
                {
                    PaintDotNet.Direct2D1.IDeviceBitmap deviceBitmap = deviceContext.CreateBitmap(size,
                                                                                                  surfaceLock.Buffer,
                                                                                                  surfaceLock.BufferStride,
                                                                                                  PaintDotNet.Dxgi.DxgiFormat.B8G8R8A8_UNorm,
                                                                                                  AlphaMode.Ignore);
                    IBitmapBrush bitmapBrush = deviceContext.CreateBitmapBrush(deviceBitmap,
                                                                               ExtendMode.Wrap,
                                                                               ExtendMode.Wrap,
                                                                               InterpolationMode.NearestNeighbor);
                    brush = new PDNDeviceBitmapBrush(deviceBitmap, bitmapBrush);
                }
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Direct2DException($"{nameof(CreateBitmapBrush)} failed", ex);
            }

            return brush;
        }

        public void DrawBitmap(IDeviceBitmap bitmap)
        {
            VerifyNotDisposed();

            try
            {
                deviceContext.DrawBitmap(((INativeResourceProperty<PaintDotNet.Direct2D1.IDeviceBitmap>)bitmap).NativeResource);
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Direct2DException($"{nameof(DrawBitmap)} failed", ex);
            }
        }

        public void EndDraw()
        {
            VerifyNotDisposed();

            try
            {
                deviceContext.EndDraw();
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (PaintDotNet.Direct2D1.RecreateTargetException)
            {
                throw new RecreateTargetException();
            }
            catch (Exception ex)
            {
                throw new Direct2DException($"{nameof(EndDraw)} failed", ex);
            }
        }

        public void FillRectangle(int x, int y, int width, int height, IDeviceBitmapBrush brush)
        {
            VerifyNotDisposed();

            try
            {
                deviceContext.FillRectangle(x, y, width, height, ((INativeResourceProperty<IBitmapBrush>)brush).NativeResource);
            }
            catch (OutOfMemoryException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new Direct2DException($"{nameof(FillRectangle)} failed", ex);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                deviceContext.Dispose();
            }
        }
    }
}
