/* Adapted from PIGeneral.h
 * Copyright (c) 1992-6, Adobe Systems Incorporated.
 * All rights reserved.
*/

using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    /// Return Type: OSErr->short
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short AdvanceStateProc();
}
