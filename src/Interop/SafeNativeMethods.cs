/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2022 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;

namespace PSFilterPdn.Interop
{
    [System.Security.SuppressUnmanagedCodeSecurity]
    internal static class SafeNativeMethods
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern int SHCreateItemFromParsingName(
            [MarshalAs(UnmanagedType.LPWStr)] string pszPath,
            IntPtr pbc,
            [In()] ref Guid riid,
            [MarshalAs(UnmanagedType.Interface, IidParameterIndex = 2)] out NativeInterfaces.IShellItem ppv
            );

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = false, ExactSpelling = true)]
        internal static extern IntPtr GetCurrentProcess();

        [DllImport("user32.dll")]
        internal static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, ref NativeStructs.TCHITTESTINFO lParam);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        internal static extern IntPtr GetProcessHeap();

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern IntPtr HeapAlloc(IntPtr hHeap, uint dwFlags, UIntPtr dwSize);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool HeapFree(IntPtr hHeap, uint dwFlags, IntPtr lpMem);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern IntPtr HeapReAlloc(IntPtr hHeap, uint dwFlags, IntPtr lpMem, UIntPtr dwBytes);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern UIntPtr HeapSize(IntPtr hHeap, uint dwFlags, IntPtr lpMem);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern unsafe UIntPtr VirtualQuery(IntPtr address, out NativeStructs.MEMORY_BASIC_INFORMATION buffer, UIntPtr sizeOfBuffer);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern IntPtr VirtualAlloc(
            IntPtr lpAddress,
            UIntPtr dwSize,
            uint flAllocationType,
            uint flProtect);

        [DllImport("kernel32.dll", ExactSpelling = true, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool VirtualFree(
            IntPtr lpAddress,
            UIntPtr dwSize,
            uint dwFreeType);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern IntPtr GlobalSize(IntPtr hMem);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern IntPtr GlobalFree(IntPtr hMem);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern IntPtr GlobalReAlloc(IntPtr hMem, UIntPtr dwBytes, uint uFlags);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GlobalMemoryStatusEx(ref NativeStructs.MEMORYSTATUSEX lpBuffer);

        [DllImport("user32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern unsafe bool SetWindowTextW(IntPtr hWnd, ushort* lpString);

        [DllImport("comdlg32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern unsafe bool ChooseColorW(ref NativeStructs.CHOOSECOLORW lppsd);
    }
}
