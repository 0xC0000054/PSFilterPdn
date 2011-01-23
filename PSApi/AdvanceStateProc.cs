using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    /// Return Type: OSErr->short
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short AdvanceStateProc();
}
