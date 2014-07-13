/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2014 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    static class NativeStructs
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION
        {
            public System.IntPtr BaseAddress;
            public System.IntPtr AllocationBase;
            public uint AllocationProtect;
            public System.UIntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }
    }
}
