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

using System;
using System.Runtime.InteropServices;
using System.Security;

namespace PSFilterPdn.Interop
{
    [SuppressUnmanagedCodeSecurity]
    internal static partial class UnsafeNativeMethods
    {
        [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
        internal static partial uint GetFileAttributesW(string lpFileName);

        [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        internal static partial SafeLibraryHandle LoadLibraryExW(string lpFileName, IntPtr hFile, uint dwFlags);

        [LibraryImport("kernel32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool EnumResourceNamesW(SafeLibraryHandle hModule,
                                                              string lpszType,
                                                              delegate* unmanaged<IntPtr, IntPtr, IntPtr, IntPtr, BOOL> lpEnumFunc,
                                                              IntPtr lParam);

        [LibraryImport("kernel32.dll")]
        internal static partial IntPtr FindResourceW(IntPtr hModule, IntPtr lpName, IntPtr lpType);

        [LibraryImport("kernel32.dll")]
        internal static partial IntPtr LoadResource(IntPtr hModule, IntPtr hResource);

        [LibraryImport("kernel32.dll")]
        internal static partial IntPtr LockResource(IntPtr hGlobal);

        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool FreeLibrary(IntPtr hModule);

        [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Custom, StringMarshallingCustomType = typeof(System.Runtime.InteropServices.Marshalling.AnsiStringMarshaller))]
        internal static partial IntPtr GetProcAddress(SafeLibraryHandle hModule, string lpProcName);
    }
}
