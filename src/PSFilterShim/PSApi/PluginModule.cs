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

using PSFilterShim.Properties;
using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal unsafe delegate void PluginEntryPoint(FilterSelector selector, void* pluginParamBlock, ref IntPtr pluginData, ref short result);

    internal sealed class PluginModule : IDisposable
    {
        /// <summary>
        /// The entry point for the FilterParmBlock and AboutRecord
        /// </summary>
        public readonly PluginEntryPoint entryPoint;
        private readonly string fileName;
        private SafeLibraryHandle handle;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginModule"/> class.
        /// </summary>
        /// <param name="fileName">The file name of the DLL to load.</param>
        /// <param name="entryPoint">The name of the entry point in the DLL.</param>
        /// <exception cref="EntryPointNotFoundException">The entry point specified by <paramref name="entryPoint"/> was not found in <paramref name="fileName"/>.</exception>
        /// <exception cref="System.IO.FileNotFoundException">The file specified by <paramref name="fileName"/> cannot be found.</exception>
        public PluginModule(string fileName, string entryPoint)
        {
            disposed = false;
            handle = UnsafeNativeMethods.LoadLibraryW(fileName);
            if (!handle.IsInvalid)
            {
                IntPtr address = UnsafeNativeMethods.GetProcAddress(handle, entryPoint);

                if (address != IntPtr.Zero)
                {
                    this.entryPoint = (PluginEntryPoint)Marshal.GetDelegateForFunctionPointer(address, typeof(PluginEntryPoint));
                    this.fileName = fileName;
                }
                else
                {
                    handle.Dispose();
                    handle = null;

                    throw new EntryPointNotFoundException(string.Format(CultureInfo.CurrentCulture, Resources.PluginEntryPointNotFound, entryPoint, fileName));
                }
            }
            else
            {
                int hr = Marshal.GetHRForLastWin32Error();
                Marshal.ThrowExceptionForHR(hr);
            }
        }

        /// <summary>
        /// Gets a delegate for the plugin entry point.
        /// </summary>
        /// <param name="entryPointName">The name of the entry point.</param>
        /// <returns>A <see cref="PluginEntryPoint" /> delegate.</returns>
        /// <exception cref="EntryPointNotFoundException">The entry point specified by <paramref name="entryPointName" /> was not found.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        public PluginEntryPoint GetEntryPoint(string entryPointName)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("PluginModule");
            }

            IntPtr address = UnsafeNativeMethods.GetProcAddress(handle, entryPointName);

            if (address == IntPtr.Zero)
            {
                throw new EntryPointNotFoundException(string.Format(CultureInfo.CurrentCulture, Resources.PluginEntryPointNotFound, entryPointName, fileName));
            }

            return (PluginEntryPoint)Marshal.GetDelegateForFunctionPointer(address, typeof(PluginEntryPoint));
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;

                if (handle != null && !handle.IsClosed)
                {
                    handle.Dispose();
                    handle = null;
                }
            }
        }
    }
}
