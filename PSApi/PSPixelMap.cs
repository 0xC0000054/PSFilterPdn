/* Adapted from PIGeneral.h
 * Copyright (c) 1992-6, Adobe Systems Incorporated.
 * All rights reserved.
*/

namespace PSFilterLoad.PSApi
{
    internal enum MaskDescription : int
    {
        kSimplePSMask = 0,
        kBlackMatPSMask = 1,
        kGrayMatPSMask = 2,
        kWhiteMatPSMask = 3,
        kInvertPSMask = 4
    }

    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct PSPixelMask
    {
        public System.IntPtr next;
        public System.IntPtr maskData;
        public int rowBytes;
        public int colBytes;
        public MaskDescription maskDescription;
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
