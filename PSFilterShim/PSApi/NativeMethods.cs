using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security;

namespace PSFilterLoad.PSApi
{
    static class NativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern SafeLibraryHandle LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeLibrary(IntPtr hModule);

        [SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "1"), DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress([In()] SafeLibraryHandle hModule, [In(), MarshalAs(UnmanagedType.LPStr)] string lpProcName);

        [DllImport("kernel32.dll", EntryPoint = "GlobalAlloc")]
        public static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [DllImport("kernel32.dll", EntryPoint = "GlobalSize")]
        public static extern IntPtr GlobalSize([In()] System.IntPtr hMem);

        [DllImport("kernel32.dll", EntryPoint = "GlobalFree")]
        public static extern IntPtr GlobalFree([In()] System.IntPtr hMem);

        [DllImport("kernel32.dll", EntryPoint = "GlobalLock")]
        public static extern IntPtr GlobalLock([In()] System.IntPtr hMem);
 
        [DllImport("kernel32.dll", EntryPoint = "GlobalReAlloc")]
        public static extern IntPtr GlobalReAlloc([In()] IntPtr hMem, UIntPtr dwBytes, uint uFlags);
        
        [DllImport("kernel32.dll", EntryPoint = "GlobalUnlock")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalUnlock([In()] System.IntPtr hMem);
     }
}
