using System.Runtime.InteropServices;
using System;

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

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
            public MEMORYSTATUSEX()
            {
                this.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
            }
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
            public UIntPtr RegionSize;

            /// DWORD->unsigned int
            public uint State;

            /// DWORD->unsigned int
            public uint Protect;

            /// DWORD->unsigned int
            public uint Type;
        }

       
    }
}
