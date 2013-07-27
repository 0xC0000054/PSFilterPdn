/* Adapted from PIGeneral.h
 * Copyright (c) 1992-6, Adobe Systems Incorporated.
 * All rights reserved.
*/

using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    /// Return Type: OSErr->short
    ///source: PSPixelMap*
    ///srcRect: VRect*
    ///dstRow: int32->int
    ///dstCol: int32->int
    ///platformContext: void*
    [UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate short DisplayPixelsProc([In()] ref PSPixelMap source, [In()] ref VRect srcRect, [In()] int dstRow, [In()] int dstCol, [In()] System.IntPtr platformContext);
}
