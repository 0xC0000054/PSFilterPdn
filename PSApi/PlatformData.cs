using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    [StructLayoutAttribute(LayoutKind.Sequential)]
    struct PlatformData
    { 
        /// <summary>
        /// The handle of the parent window
        /// </summary>
        public IntPtr hwnd;
    }
}
