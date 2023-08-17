﻿/////////////////////////////////////////////////////////////////////////////////
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

using TerraFX.Interop.Windows;
using System;
using System.Runtime.InteropServices;
using System.ComponentModel;

#nullable enable

using static TerraFX.Interop.Windows.Windows;

namespace PSFilterLoad.PSApi.Imaging.Internal
{
    internal sealed unsafe class WICFactory : Disposable, IWICFactory
    {
        // Try to ensure that we load Windowscodecs.dll from the System32 directory.
        // This is only a best faith effort as it is possible that the DLL was already loaded into the process.
        private static readonly HMODULE WindowsCodecsModuleHandle = LoadWindowsCodecsFromSystem32();
        private readonly ComPtr<IWICImagingFactory> factory;

        public WICFactory()
        {
            factory = default;
            Guid clsid = CLSID.CLSID_WICImagingFactory2;

            HRESULT hr = CoCreateInstance(&clsid,
                                          null,
                                          (uint)CLSCTX.CLSCTX_INPROC_SERVER,
                                          __uuidof<IWICImagingFactory2>(),
                                          (void**)factory.GetAddressOf());
            if (hr.FAILED)
            {
                Marshal.ThrowExceptionForHR(hr.Value);
            }
        }

        public IWICImagingFactory* Get()
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

        private static HMODULE LoadWindowsCodecsFromSystem32()
        {
            HMODULE module = HMODULE.NULL;

            fixed (char* lpFileName = "Windowscodecs.dll")
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
