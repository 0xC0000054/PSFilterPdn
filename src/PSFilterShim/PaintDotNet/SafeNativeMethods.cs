using System;
using System.Runtime.InteropServices;

namespace PaintDotNet.SystemLayer
{
    [System.Security.SuppressUnmanagedCodeSecurity]
    internal static partial class SafeNativeMethods
    {
        [LibraryImport("Kernel32.dll", SetLastError = false)]
        internal static partial IntPtr HeapAlloc(IntPtr hHeap, uint dwFlags, UIntPtr dwBytes);

        [LibraryImport("Kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool HeapFree(IntPtr hHeap, uint dwFlags, IntPtr lpMem);

        [LibraryImport("Kernel32.dll", SetLastError = false)]
        internal static partial UIntPtr HeapSize(IntPtr hHeap, uint dwFlags, IntPtr lpMem);

        [LibraryImport("Kernel32.dll", SetLastError = true)]
        internal static partial IntPtr HeapCreate(uint flOptions, UIntPtr dwInitialSize, UIntPtr dwMaximumSize);

        [LibraryImport("Kernel32.dll", SetLastError = true)]
        internal static partial uint HeapDestroy(IntPtr hHeap);

        [LibraryImport("Kernel32.Dll", SetLastError = true)]
        internal static unsafe partial uint HeapSetInformation(
            IntPtr HeapHandle,
            int HeapInformationClass,
            void* HeapInformation,
            UIntPtr HeapInformationLength
            );

        [LibraryImport("kernel32.dll", SetLastError = true)]
        internal static partial IntPtr VirtualAlloc(
            IntPtr lpAddress,
            UIntPtr dwSize,
            uint flAllocationType,
            uint flProtect);

        [LibraryImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static partial bool VirtualFree(
            IntPtr lpAddress,
            UIntPtr dwSize,
            uint dwFreeType);
    }
}
