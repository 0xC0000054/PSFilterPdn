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

namespace PSFilterLoad.PSApi.Interop
{
    [System.Security.SuppressUnmanagedCodeSecurity]
    internal static partial class SafeNativeMethods
    {
        [LibraryImport("kernel32.dll", SetLastError = true)]
        internal static partial IntPtr GetProcessHeap();

        [LibraryImport("kernel32.dll")]
        internal static partial IntPtr HeapAlloc(IntPtr hHeap, uint dwFlags, UIntPtr dwSize);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool HeapFree(IntPtr hHeap, uint dwFlags, IntPtr lpMem);

        [LibraryImport("kernel32.dll")]
        internal static partial IntPtr HeapReAlloc(IntPtr hHeap, uint dwFlags, IntPtr lpMem, UIntPtr dwBytes);

        [LibraryImport("kernel32.dll")]
        internal static partial UIntPtr HeapSize(IntPtr hHeap, uint dwFlags, IntPtr lpMem);

        [LibraryImport("kernel32.dll")]
        internal static unsafe partial UIntPtr VirtualQuery(IntPtr address, out NativeStructs.MEMORY_BASIC_INFORMATION buffer, UIntPtr sizeOfBuffer);

        [LibraryImport("kernel32.dll")]
        internal static partial IntPtr VirtualAlloc(IntPtr lpAddress, UIntPtr dwSize, uint flAllocationType, uint flProtect);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool VirtualFree(IntPtr lpAddress, UIntPtr dwSize, uint dwFreeType);

        [LibraryImport("kernel32.dll")]
        internal static partial IntPtr GlobalSize(IntPtr hMem);

        [LibraryImport("kernel32.dll")]
        internal static partial IntPtr GlobalFree(IntPtr hMem);

        [LibraryImport("kernel32.dll")]
        internal static partial IntPtr GlobalLock(IntPtr hMem);

        [LibraryImport("kernel32.dll")]
        internal static partial IntPtr GlobalReAlloc(IntPtr hMem, UIntPtr dwBytes, uint uFlags);

        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GlobalUnlock(IntPtr hMem);

        [LibraryImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool GlobalMemoryStatusEx(ref NativeStructs.MEMORYSTATUSEX lpBuffer);

        [LibraryImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool SetWindowTextW(IntPtr hWnd, ushort* lpString);

        [LibraryImport("comdlg32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static unsafe partial bool ChooseColorW(ref NativeStructs.CHOOSECOLORW lppsd);
    }
}
