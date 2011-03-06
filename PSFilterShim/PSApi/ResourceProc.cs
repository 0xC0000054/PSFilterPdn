using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{

    /// Return Type: int16->short
    ///type: ResType->unsigned int
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short CountPIResourcesProc(uint type);

    /// Return Type: Handle->LPSTR*
    ///type: ResType->unsigned int
    ///index: int16->short
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate System.IntPtr GetPIResourceProc(uint type, short index);

    /// Return Type: void
    ///type: ResType->unsigned int
    ///index: int16->short
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void DeletePIResourceProc(uint type, short index);

    /// Return Type: OSErr->short
    ///type: ResType->unsigned int
    ///data: Handle->LPSTR*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short AddPIResourceProc(uint type, ref System.IntPtr data);

    [StructLayout(LayoutKind.Sequential)]
    internal struct ResourceProcs
    {
        /// int16->short
        public short resourceProcsVersion;

        /// int16->short
        public short numResourceProcs;

        /// CountPIResourcesProc
        public IntPtr countProc;

        /// GetPIResourceProc
        public IntPtr getProc;

        /// DeletePIResourceProc
        public IntPtr deleteProc;

        /// AddPIResourceProc
        public IntPtr addProc;
    }
}
