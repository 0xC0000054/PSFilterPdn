/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2024 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using TerraFX.Interop.Windows;
using System;
using System.Buffers;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

using static TerraFX.Interop.Windows.Windows;

namespace PSFilterLoad.PSApi
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal unsafe delegate void PluginEntryPoint(FilterSelector selector, void* pluginParamBlock, ref IntPtr pluginData, ref short result);

    internal sealed unsafe class PluginModule : Disposable
    {
        /// <summary>
        /// The entry point for the FilterParmBlock and AboutRecord
        /// </summary>
        public readonly PluginEntryPoint entryPoint;
        private readonly string fileName;
        private HMODULE handle;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginModule"/> class.
        /// </summary>
        /// <param name="fileName">The file name of the DLL to load.</param>
        /// <param name="entryPoint">The name of the entry point in the DLL.</param>
        /// <exception cref="EntryPointNotFoundException">The entry point specified by <paramref name="entryPoint"/> was not found in <paramref name="fileName"/>.</exception>
        /// <exception cref="Win32Exception">Failed to load the plugin library.</exception>
        public PluginModule(string fileName, string entryPoint)
        {
            fixed (char* lpFileName = fileName)
            {
                handle = LoadLibraryExW(lpFileName, HANDLE.NULL, 0U);
            }

            if (handle != HANDLE.NULL)
            {
                try
                {
                    this.entryPoint = GetEntryPointDelegate(entryPoint);
                    this.fileName = fileName;
                }
                catch (Exception)
                {
                    Dispose();
                    throw;
                }
            }
            else
            {
                int error = Marshal.GetLastSystemError();
                throw new Win32Exception(error);
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
            VerifyNotDisposed();

            return GetEntryPointDelegate(entryPointName);
        }

        protected override void Dispose(bool disposing)
        {
            if (handle != HMODULE.NULL)
            {
                FreeLibrary(handle);
                handle = HMODULE.NULL;
            }
        }

        /// <summary>
        /// Gets the entry point delegate.
        /// </summary>
        /// <param name="name">The entry point name.</param>
        /// <returns>The entry point delegate.</returns>
        /// <exception cref="EntryPointNotFoundException">he entry point specified by <paramref name="name" /> was not found.</exception>
        [SkipLocalsInit]
        private PluginEntryPoint GetEntryPointDelegate(string name)
        {
            const int MaxStackAllocBufferSize = 128;

            Span<byte> buffer = stackalloc byte[MaxStackAllocBufferSize];
            byte[]? bufferFromPool = null;

            PluginEntryPoint entryPoint;

            try
            {
                int asciiNameLength = Encoding.ASCII.GetByteCount(name);

                int asciiNameLengthWithTerminator = checked(asciiNameLength + 1);

                if (asciiNameLengthWithTerminator > MaxStackAllocBufferSize)
                {
                    bufferFromPool = ArrayPool<byte>.Shared.Rent(asciiNameLengthWithTerminator);
                    buffer = bufferFromPool;
                }

                buffer = buffer.Slice(0, asciiNameLengthWithTerminator);

                int bytesWritten = Encoding.ASCII.GetBytes(name, buffer);

                // Add the NUL-terminator.
                buffer[bytesWritten] = 0;

                fixed (byte* lpProcName = buffer)
                {
                    void* address = GetProcAddress(handle, (sbyte*)lpProcName);

                    if (address == null)
                    {
                        throw new EntryPointNotFoundException(string.Format(CultureInfo.CurrentCulture,
                                                                            StringResources.PluginEntryPointNotFound,
                                                                            name,
                                                                            fileName));
                    }

                    entryPoint = Marshal.GetDelegateForFunctionPointer<PluginEntryPoint>((nint)address);
                }
            }
            finally
            {
                if (bufferFromPool != null)
                {
                    ArrayPool<byte>.Shared.Return(bufferFromPool);
                }
            }

            return entryPoint;
        }
    }
}
