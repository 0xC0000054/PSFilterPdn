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

using PSFilterLoad.PSApi.Imaging;
using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using System;

using static TerraFX.Interop.DirectX.DXGI_FORMAT;
using static TerraFX.Interop.DirectX.D2D1_ALPHA_MODE;
using static TerraFX.Interop.DirectX.Pointers;
using static TerraFX.Interop.Windows.Windows;

namespace PSFilterLoad.PSApi.Rendering.Internal
{
    internal sealed unsafe class DeviceContextRenderTarget : Disposable, IDeviceContextRenderTarget
    {
        private readonly ComPtr<ID2D1DCRenderTarget> renderTarget;
        private readonly ComPtr<ID2D1DeviceContext> d2d1DeviceContext;

        public DeviceContextRenderTarget(IDirect2DFactory factory, bool maybeHasTransparency)
        {
            D2D1_ALPHA_MODE alphaMode = maybeHasTransparency ? D2D1_ALPHA_MODE_PREMULTIPLIED : D2D1_ALPHA_MODE_IGNORE;
            D2D1_PIXEL_FORMAT pixelFormat = new(DXGI_FORMAT_B8G8R8A8_UNORM, alphaMode);
            D2D1_RENDER_TARGET_PROPERTIES properties = new(D2D1_RENDER_TARGET_TYPE.D2D1_RENDER_TARGET_TYPE_DEFAULT,
                                                           pixelFormat,
                                                           96.0f,
                                                           96.0f,
                                                           D2D1_RENDER_TARGET_USAGE.D2D1_RENDER_TARGET_USAGE_GDI_COMPATIBLE);

            HRESULT hr = factory.Get()->CreateDCRenderTarget(&properties, renderTarget.GetAddressOf());
            Direct2DException.ThrowIfFailed("Failed to create the ID2D1DCRenderTarget.", hr);

            try
            {
                hr = renderTarget.Get()->QueryInterface(__uuidof<ID2D1DeviceContext>(), (void**)d2d1DeviceContext.GetAddressOf());
                Direct2DException.ThrowIfFailed("Failed to get the ID2D1DeviceContext.", hr);
            }
            catch (Exception)
            {
                Dispose();
                throw;
            }
        }

        public D2D1_PRIMITIVE_BLEND PrimitiveBlend
        {
            get
            {
                VerifyNotDisposed();

                return d2d1DeviceContext.Get()->GetPrimitiveBlend();
            }
            set
            {
                VerifyNotDisposed();

                d2d1DeviceContext.Get()->SetPrimitiveBlend(value);
            }
        }

        public bool SupportsTransparency
        {
            get
            {
                VerifyNotDisposed();

                return renderTarget.Get()->GetPixelFormat().alphaMode == D2D1_ALPHA_MODE_PREMULTIPLIED;
            }
        }

        public void BeginDraw()
        {
            VerifyNotDisposed();

            renderTarget.Get()->BeginDraw();
        }

        public void BindToDeviceContext(HDC deviceContextHandle, RECT bounds)
        {
            VerifyNotDisposed();

            HRESULT hr = renderTarget.Get()->BindDC(deviceContextHandle, &bounds);
            Direct2DException.ThrowIfFailed($"{nameof(BindToDeviceContext)} failed.", hr);
        }

        public IDeviceBitmap CreateBitmap(DisplayPixelsSurface surface, bool ignoreAlpha)
        {
            VerifyNotDisposed();

            D2D_SIZE_U bitmapSize = new((uint)surface.Width, (uint)surface.Height);

            D2D1_ALPHA_MODE alphaMode = ignoreAlpha ? D2D1_ALPHA_MODE_IGNORE : D2D1_ALPHA_MODE_PREMULTIPLIED;
            D2D1_PIXEL_FORMAT pixelFormat = new(DXGI_FORMAT_B8G8R8A8_UNORM, alphaMode);

            D2D1_BITMAP_PROPERTIES properties = new(pixelFormat);

            IDeviceBitmap deviceBitmap;
            ComPtr<ID2D1Bitmap> nativeBitmap = default;

            try
            {
                using (ISurfaceLock surfaceLock = surface.Lock(SurfaceLockMode.Read))
                {
                    HRESULT hr = renderTarget.Get()->CreateBitmap(bitmapSize,
                                                                  surfaceLock.Buffer,
                                                                  (uint)surfaceLock.BufferStride,
                                                                  &properties,
                                                                  nativeBitmap.GetAddressOf());
                    Direct2DException.ThrowIfFailed($"{nameof(CreateBitmap)} failed.", hr);

                    deviceBitmap = new DeviceBitmap(ref nativeBitmap);
                }
            }
            finally
            {
                nativeBitmap.Dispose();
            }

            return deviceBitmap;
        }

        public IDeviceBitmapBrush CreateBitmapBrush(TransparencyCheckerboardSurface surface)
        {
            VerifyNotDisposed();

            D2D_SIZE_U bitmapSize = new((uint)surface.Width, (uint)surface.Height);

            D2D1_PIXEL_FORMAT pixelFormat = new(DXGI_FORMAT_B8G8R8A8_UNORM, D2D1_ALPHA_MODE_IGNORE);
            D2D1_BITMAP_PROPERTIES bitmapProperties = new(pixelFormat);

            D2D1_BITMAP_BRUSH_PROPERTIES brushProperties = new(D2D1_EXTEND_MODE.D2D1_EXTEND_MODE_WRAP,
                                                               D2D1_EXTEND_MODE.D2D1_EXTEND_MODE_WRAP,
                                                               D2D1_BITMAP_INTERPOLATION_MODE.D2D1_BITMAP_INTERPOLATION_MODE_NEAREST_NEIGHBOR);

            IDeviceBitmapBrush deviceBitmapBrush;
            ComPtr<ID2D1Bitmap> nativeBitmap = default;
            ComPtr<ID2D1BitmapBrush> nativeBitmapBrush = default;

            try
            {
                using (ISurfaceLock surfaceLock = surface.Lock(SurfaceLockMode.Read))
                {
                    HRESULT hr = renderTarget.Get()->CreateBitmap(bitmapSize,
                                                                  surfaceLock.Buffer,
                                                                  (uint)surfaceLock.BufferStride,
                                                                  &bitmapProperties,
                                                                  nativeBitmap.GetAddressOf());
                    Direct2DException.ThrowIfFailed($"{nameof(CreateBitmapBrush)} failed.", hr);

                    hr = renderTarget.Get()->CreateBitmapBrush(nativeBitmap.Get(), &brushProperties, nativeBitmapBrush.GetAddressOf());
                    Direct2DException.ThrowIfFailed($"{nameof(CreateBitmapBrush)} failed.", hr);

                    deviceBitmapBrush = new DeviceBitmapBrush(ref nativeBitmap, ref nativeBitmapBrush);
                }
            }
            finally
            {
                nativeBitmap.Dispose();
                nativeBitmapBrush.Dispose();
            }

            return deviceBitmapBrush;
        }

        public void DrawBitmap(IDeviceBitmap bitmap)
        {
            VerifyNotDisposed();

            ID2D1Bitmap* nativeBitmap = ((INativeResourceProperty<ID2D1Bitmap>)bitmap).NativeResource;

            renderTarget.Get()->DrawBitmap(nativeBitmap);
        }

        public void EndDraw()
        {
            VerifyNotDisposed();

            HRESULT hr = renderTarget.Get()->EndDraw();
            Direct2DException.ThrowIfFailed($"{nameof(EndDraw)} failed.", hr);
        }

        public void FillRectangle(int x, int y, int width, int height, IDeviceBitmapBrush brush)
        {
            VerifyNotDisposed();

            ID2D1BitmapBrush* bitmapBrush = ((INativeResourceProperty<ID2D1BitmapBrush>)brush).NativeResource;

            D2D_RECT_F rect = new(x, y, x + width, y + height);

            renderTarget.Get()->FillRectangle(&rect, __cast(bitmapBrush));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                d2d1DeviceContext.Dispose();
                renderTarget.Dispose();
            }
        }
    }
}
