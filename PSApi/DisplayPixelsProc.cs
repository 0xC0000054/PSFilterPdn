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
    internal delegate short DisplayPixelsProc(ref PSPixelMap source, ref VRect srcRect, int dstRow, int dstCol,[In, Out] System.IntPtr platformContext);
}
