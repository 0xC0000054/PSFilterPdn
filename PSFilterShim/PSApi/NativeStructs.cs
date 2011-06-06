using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    static class NativeStructs
    {
        [StructLayoutAttribute(LayoutKind.Sequential)]
        public struct BITMAPINFO
        {
            public BITMAPINFOHEADER bmiHeader;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 1, ArraySubType = UnmanagedType.Struct)]
            public RGBQUAD[] bmiColors;
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        public struct BITMAPINFOHEADER
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public uint biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        public struct RGBQUAD
        {
            public byte rgbBlue;
            public byte rgbGreen;
            public byte rgbRed;
            public byte rgbReserved;
        }

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
