/* Adapted from PIGeneral.h
 * Copyright (c) 1992-6, Adobe Systems Incorporated.
 * All rights reserved.
*/

namespace PSFilterLoad.PSApi
{

    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct PSPixelMask
    {

        /// PSPixelMask*
        public System.IntPtr next;

        /// void*
        public System.IntPtr maskData;

        /// int32->int
        public int rowBytes;

        /// int32->int
        public int colBytes;

        /// int32->int
        public int maskDescription;
    }

    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct PSPixelMap
    {
        public int version;
        public VRect bounds;
        public int imageMode;
        public int rowBytes;
        public int colBytes;
        public int planeBytes;
        public System.IntPtr baseAddr;
        public System.IntPtr mat;
        public System.IntPtr masks;
        public int maskPhaseRow;
        public int maskPhaseCol;
    }
}
