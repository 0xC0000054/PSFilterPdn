/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2015 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    [System.Security.SuppressUnmanagedCodeSecurity]
    internal static class SafeNativeMethods
    {
        [DllImport("kernel32.dll", SetLastError = false)]
        internal static extern IntPtr GetProcessHeap();

        [DllImport("kernel32.dll", SetLastError = false), ]
        internal static extern IntPtr HeapAlloc(IntPtr hHeap, uint dwFlags, UIntPtr dwSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool HeapFree(IntPtr hHeap, uint dwFlags, IntPtr lpMem);

        [DllImport("kernel32.dll", SetLastError = false)]
        internal static extern IntPtr HeapReAlloc(IntPtr hHeap, uint dwFlags, IntPtr lpMem, UIntPtr dwBytes);

        [DllImport("kernel32.dll", SetLastError = false)]
        internal static extern UIntPtr HeapSize(IntPtr hHeap, uint dwFlags, IntPtr lpMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern unsafe UIntPtr VirtualQuery(IntPtr address, ref NativeStructs.MEMORY_BASIC_INFORMATION buffer, UIntPtr sizeOfBuffer);

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        internal static extern IntPtr memset(IntPtr dest, int c, UIntPtr count);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr VirtualAlloc(IntPtr lpAddress, UIntPtr dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool VirtualFree(IntPtr lpAddress, UIntPtr dwSize, uint dwFreeType);

        [DllImport("kernel32.dll", EntryPoint = "GlobalSize")]
        internal static extern IntPtr GlobalSize([In()] System.IntPtr hMem);

        [DllImport("kernel32.dll", EntryPoint = "GlobalFree")]
        internal static extern IntPtr GlobalFree([In()] System.IntPtr hMem);

        [DllImport("kernel32.dll", EntryPoint = "GlobalLock")]
        internal static extern IntPtr GlobalLock([In()] System.IntPtr hMem);

        [DllImport("kernel32.dll", EntryPoint = "GlobalReAlloc")]
        internal static extern IntPtr GlobalReAlloc([In()] IntPtr hMem, UIntPtr dwBytes, uint uFlags);

        [DllImport("kernel32.dll", EntryPoint = "GlobalUnlock")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GlobalUnlock([In()] System.IntPtr hMem);

        [DllImport("gdi32.dll", ExactSpelling = true)]
        internal unsafe static extern uint GetRegionData([In()] IntPtr hrgn, [In()] uint nCount, [Out()] NativeStructs.RGNDATA* lpRgnData);

        [DllImport("gdi32.dll", ExactSpelling = true)]
        internal static extern IntPtr CreateCompatibleDC([In()] IntPtr hdc);

        [DllImport("gdi32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteObject([In()] IntPtr hdc);

        [DllImport("gdi32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool DeleteDC([In()] IntPtr hdc);
    }
}
