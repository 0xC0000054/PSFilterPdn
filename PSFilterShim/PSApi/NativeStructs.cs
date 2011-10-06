using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    static class NativeStructs
    {
        [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION
        {

            /// PVOID->void*
            public System.IntPtr BaseAddress;

            /// PVOID->void*
            public System.IntPtr AllocationBase;

            /// DWORD->unsigned int
            public uint AllocationProtect;

            /// SIZE_T->ULONG_PTR->unsigned int
            public System.UIntPtr RegionSize;

            /// DWORD->unsigned int
            public uint State;

            /// DWORD->unsigned int
            public uint Protect;

            /// DWORD->unsigned int
            public uint Type;
        }
    }
}
