using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    [System.Security.SuppressUnmanagedCodeSecurity]
    internal static class UnsafeNativeMethods
    {
        internal delegate bool EnumResNameDelegate([In()] IntPtr hModule, [In()] IntPtr lpszType, [In()] IntPtr lpszName, [In()] IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern SafeLibraryHandle LoadLibraryExW([In()] string lpFileName, IntPtr hFile, uint dwFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EnumResourceNamesW([In()] IntPtr hModule, [In()] string lpszType, EnumResNameDelegate lpEnumFunc, [In()] IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern IntPtr FindResourceW([In()] IntPtr hModule, [In()] IntPtr lpName, [In()] IntPtr lpType);

        [DllImport("kernel32.dll", EntryPoint = "LoadResource")]
        internal static extern IntPtr LoadResource([In()] IntPtr hModule, [In()] IntPtr hResource);

        [DllImport("kernel32.dll", EntryPoint = "LockResource")]
        internal static extern IntPtr LockResource([In()] IntPtr hGlobal);

        [DllImport("kernel32.dll", EntryPoint = "FreeLibrary")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FreeLibrary([In()] IntPtr hModule);

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, BestFitMapping = false, SetLastError = true)]
        internal static extern IntPtr GetProcAddress([In()] SafeLibraryHandle hModule, [In(), MarshalAs(UnmanagedType.LPStr)] string lpProcName);
    }
}
