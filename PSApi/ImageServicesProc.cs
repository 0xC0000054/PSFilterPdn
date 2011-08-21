/* Adapted from PIGeneral.h
 * Copyright (c) 1992-6, Adobe Systems Incorporated.
 * All rights reserved.
*/

using System;
using System.Runtime.InteropServices;
namespace PSFilterLoad.PSApi
{
#if PSSDK_3_0_4 && USEIMAGESERVICES

    /// Return Type: OSErr->short
    ///source: PSImagePlane*
    ///destination: PSImagePlane*
    ///area: Rect*
    ///coords: Fixed*
    ///method: int16->short
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PIResampleProc(ref PSImagePlane source, ref PSImagePlane destination, ref Rect16 area, IntPtr coords, short method);

    [StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    internal struct ImageServicesProcs
    {
        /// int16->short
        public short imageServicesProcsVersion;

        /// int16->short
        public short numImageServicesProcs;

        /// PIResampleProc
        public IntPtr interpolate1DProc;

        /// PIResampleProc
        public IntPtr interpolate2DProc;

    }
    
#endif


}
