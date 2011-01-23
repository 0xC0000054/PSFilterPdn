using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{


    [StructLayout(LayoutKind.Sequential)]
    internal struct PSImagePlane
    {

        /// void*
        public IntPtr data;

        /// Rect
        public Rect16 bounds;

        /// int32->int
        public int rowBytes;

        /// int32->int
        public int colBytes;
    }
    [StructLayout(LayoutKind.Sequential)]
    internal struct PSImageMultiPlane
    {

        /// void*
        public IntPtr data;

        /// Rect
        public Rect16 bounds;

        /// int32->int
        public int rowBytes;

        /// int32->int
        public int colBytes;

        /// int16->short
        public short activeChannels;
    }
}
