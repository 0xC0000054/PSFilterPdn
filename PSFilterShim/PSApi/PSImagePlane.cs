/* Adapted from PIGeneral.h
 * Copyright (c) 1992-6, Adobe Systems Incorporated.
 * All rights reserved.
*/

using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
#if USEIMAGESERVICES
    [StructLayout(LayoutKind.Sequential)]
    internal struct PSImagePlane
    {
        public IntPtr data;
        public Rect16 bounds;
        public int rowBytes;
        public int colBytes;
    }
#endif
}
