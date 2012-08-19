using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace PaintDotNet.SystemLayer
{
    [System.Security.SuppressUnmanagedCodeSecurity]
    internal static class SafeNativeMethods
    {
        [DllImport("Kernel32.dll", SetLastError = false)]
        internal static extern IntPtr HeapAlloc(IntPtr hHeap, uint dwFlags, UIntPtr dwBytes);

        [DllImport("Kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool HeapFree(IntPtr hHeap, uint dwFlags, IntPtr lpMem);

        [DllImport("Kernel32.dll", SetLastError = false)]
        internal static extern UIntPtr HeapSize(IntPtr hHeap, uint dwFlags, IntPtr lpMem);

        [DllImport("Kernel32.dll", SetLastError = true)]
        internal static extern IntPtr HeapCreate(
            uint flOptions,
            [MarshalAs(UnmanagedType.SysUInt)] IntPtr dwInitialSize,
            [MarshalAs(UnmanagedType.SysUInt)] IntPtr dwMaximumSize
            );

        [DllImport("Kernel32.dll", SetLastError = true)]
        internal static extern uint HeapDestroy(IntPtr hHeap);

        [DllImport("Kernel32.Dll", SetLastError = true)]
        internal unsafe static extern uint HeapSetInformation(
            IntPtr HeapHandle,
            int HeapInformationClass,
            void* HeapInformation,
            uint HeapInformationLength
            );

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        internal static extern unsafe void memcpy(void* dst, void* src, UIntPtr length);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr VirtualAlloc(
            IntPtr lpAddress,
            UIntPtr dwSize,
            uint flAllocationType,
            uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool VirtualFree(
            IntPtr lpAddress,
            UIntPtr dwSize,
            uint dwFreeType);


        [DllImport("user32.dll", SetLastError = false)]
        internal static extern IntPtr SendMessageW(
            IntPtr hWnd,
            uint msg,
            IntPtr wParam,
            IntPtr lParam);
    }
}
