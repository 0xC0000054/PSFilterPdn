/* Adapted from PITypes.h
 * Copyright (c) 1992-6, Adobe Systems Incorporated.
 * All rights reserved.
*/
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct VPoint
    {

        /// int32->int
        public int v;

        /// int32->int
        public int h;
    }
 
}
