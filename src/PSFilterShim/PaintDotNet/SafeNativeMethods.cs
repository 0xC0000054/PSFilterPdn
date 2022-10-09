using System;
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
        internal static extern IntPtr HeapCreate(uint flOptions, UIntPtr dwInitialSize, UIntPtr dwMaximumSize);

        [DllImport("Kernel32.dll", SetLastError = true)]
        internal static extern uint HeapDestroy(IntPtr hHeap);

        [DllImport("Kernel32.Dll", SetLastError = true)]
        internal unsafe static extern uint HeapSetInformation(
            IntPtr HeapHandle,
            int HeapInformationClass,
            void* HeapInformation,
            UIntPtr HeapInformationLength
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
    }
}
