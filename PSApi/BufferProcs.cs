using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    /// Return Type: OSErr->short
    ///size: int32->int
    ///bufferID: BufferID*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short AllocateBufferProc(int size, ref System.IntPtr bufferID);
    /// Return Type: Ptr->LPSTR->CHAR*
    ///bufferID: BufferID->PSBuffer*
    ///moveHigh: Boolean->BYTE->unsigned char
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr LockBufferProc(IntPtr bufferID, byte moveHigh);

    /// Return Type: void
    ///bufferID: BufferID->PSBuffer*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void UnlockBufferProc(IntPtr bufferID);

    /// Return Type: void
    ///bufferID: BufferID->PSBuffer*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void FreeBufferProc(IntPtr bufferID);

    /// Return Type: int32->int
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int BufferSpaceProc();

    [StructLayout(LayoutKind.Sequential)]
    struct BufferProcs
    {
        /// int16->short
        public short bufferProcsVersion;
        /// int16->short
        public short numBufferProcs;
        /// AllocateBufferProc
        public IntPtr allocateProc;
        /// LockBufferProc
        public IntPtr lockProc;
        /// UnlockBufferProc
        public IntPtr unlockProc;
        /// FreeBufferProc
        public IntPtr freeProc;
        /// BufferSpaceProc
        public IntPtr spaceProc;
    }

}
