/* Adapted from PIAbout.h
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/
using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    [StructLayout(LayoutKind.Sequential)]
    unsafe struct AboutRecord
    {
        public IntPtr platformData;
        public IntPtr sSPBasic;
        public IntPtr plugInRef;

        public fixed byte reserved[244];
    }

}
