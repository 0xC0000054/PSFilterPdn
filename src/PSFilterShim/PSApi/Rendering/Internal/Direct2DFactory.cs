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

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using System.ComponentModel;
using System.Runtime.InteropServices;

using static TerraFX.Interop.DirectX.D2D1_FACTORY_TYPE;
using static TerraFX.Interop.DirectX.DirectX;
using static TerraFX.Interop.Windows.Windows;

namespace PSFilterLoad.PSApi.Rendering.Internal
{
    internal sealed unsafe class Direct2DFactory : Disposable, IDirect2DFactory
    {
        // Try to ensure that we load d2d1.dll from the System32 directory.
        // This is only a best faith effort as it is possible that the DLL was already loaded into the process.
        private static readonly HMODULE D2D1ModuleHandle = LoadD2D1FromSystem32();
        private readonly ComPtr<ID2D1Factory> factory;

        public Direct2DFactory()
        {
            factory = default;

            HRESULT hr = D2D1CreateFactory<ID2D1Factory>(D2D1_FACTORY_TYPE_MULTI_THREADED, (void**)factory.GetAddressOf());
            Direct2DException.ThrowIfFailed("Failed to create the ID2D1Factory.", hr);
        }

        public ID2D1Factory* Get()
        {
            VerifyNotDisposed();

            return factory.Get();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                factory.Dispose();
            }
        }

        private static HMODULE LoadD2D1FromSystem32()
        {
            HMODULE module = HMODULE.NULL;

            fixed (char* lpFileName = "d2d1.dll")
            {
                module = LoadLibraryExW((ushort*)lpFileName, HANDLE.NULL, LOAD.LOAD_LIBRARY_SEARCH_SYSTEM32);

                if (module == HMODULE.NULL)
                {
                    int lastError = Marshal.GetLastSystemError();
                    throw new Win32Exception(lastError);
                }
            }

            return module;
        }
    }
}
