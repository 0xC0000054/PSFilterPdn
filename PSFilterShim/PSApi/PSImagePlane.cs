/* Adapted from PIGeneral.h
 * Copyright (c) 1992-6, Adobe Systems Incorporated.
 * All rights reserved.
*/

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
    
}
