using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    [System.Security.SuppressUnmanagedCodeSecurity]
    internal static class SafeNativeMethods
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetWindowText(IntPtr hWnd, string lpString);

        [DllImport("kernel32.dll", SetLastError = false)]
        internal static extern IntPtr GetProcessHeap();

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr HeapAlloc(IntPtr hHeap, uint dwFlags, UIntPtr dwSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool HeapFree(IntPtr hHeap, uint dwFlags, IntPtr lpMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr HeapReAlloc(IntPtr hHeap, uint dwFlags, IntPtr lpMem, UIntPtr dwBytes);

        [DllImport("kernel32.dll")]
        internal static extern UIntPtr HeapSize(IntPtr hHeap, uint dwFlags, IntPtr lpMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern unsafe IntPtr VirtualQuery(IntPtr address, ref NativeStructs.MEMORY_BASIC_INFORMATION buffer, IntPtr sizeOfBuffer);

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        internal static extern IntPtr memset(IntPtr dest, int c, UIntPtr count);
    }
}
