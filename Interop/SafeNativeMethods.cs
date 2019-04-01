/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2019 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;

namespace PSFilterPdn
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

        [DllImport("kernel32.dll", EntryPoint = "GetProcessDEPPolicy")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetProcessDEPPolicy(
            [In()] IntPtr hProcess,
            [Out()] out NativeEnums.ProcessDEPPolicy lpFlags,
            [Out()] out int lpPermanent
            );

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = false, ExactSpelling = true)]
        internal static extern IntPtr GetCurrentProcess();

        [DllImport("user32.dll")]
        internal static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, ref NativeStructs.TCHITTESTINFO lParam);

        [DllImport("user32.dll", ExactSpelling = true)]
        internal static extern SafeDCHandle GetDC(IntPtr hWnd);

        [DllImport("user32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ReleaseDC(IntPtr hdc);

        [DllImport("gdi32.dll", ExactSpelling = true)]
        internal static extern int GetDeviceCaps(SafeDCHandle hdc, int nIndex);
    }
}
