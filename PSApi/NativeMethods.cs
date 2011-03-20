using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    internal delegate bool EnumResNameDelegate(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam);
    static class NativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, uint dwFlags);

        [DllImport("kernel32.dll", EntryPoint = "EnumResourceNamesW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumResourceNames([In]IntPtr hModule, [In()]string lpszType, EnumResNameDelegate lpEnumFunc, [MarshalAs(UnmanagedType.SysInt)]IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindResource([In]IntPtr hModule, [In()]IntPtr lpName, [In()]IntPtr lpType);
        
        [DllImport("Kernel32.dll", EntryPoint = "LoadResource", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResource);

        [DllImport("Kernel32.dll", EntryPoint = "LockResource", CharSet = CharSet.Unicode)]
        public static extern IntPtr LockResource(IntPtr hGlobal);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeLibrary(IntPtr hModule);

        [SuppressMessage("Microsoft.Globalization", "CA2101:SpecifyMarshalingForPInvokeStringArguments", MessageId = "1"), DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress([In()] IntPtr hModule, [In()] string lpProcName);

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

        [DllImport("kernel32.dll", EntryPoint = "IsBadReadPtr")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsBadReadPtr([In()] IntPtr lp, UIntPtr ucb);

        [DllImport("kernel32.dll", EntryPoint = "IsBadWritePtr")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsBadWritePtr([In()] IntPtr lp, UIntPtr ucb);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowText(IntPtr hWnd, string lpString);

        /// Return Type: BOOL->int
        ///lpBuffer: LPMEMORYSTATUSEX->_MEMORYSTATUSEX*
        [DllImport("kernel32.dll", EntryPoint = "GlobalMemoryStatusEx")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GlobalMemoryStatusEx([In, Out] NativeStructs.MEMORYSTATUSEX lpBuffer);

        [DllImport("kernel32.dll", EntryPoint = "LocalSize")]
        public static extern UIntPtr LocalSize([In()] System.IntPtr hMem);
 
    }
}
