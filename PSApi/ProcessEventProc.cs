/* Adapted from PIGeneral.h
 * Copyright (c) 1992-6, Adobe Systems Incorporated.
 * All rights reserved.
*/


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    /// Return Type: void
    ///event: void*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void ProcessEventProc(System.IntPtr @event);
}
