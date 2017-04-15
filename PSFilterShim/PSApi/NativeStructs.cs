/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2017 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See src/Resources/Files/License.txt for full licensing and attribution      //
// details.                                                                    //
// .                                                                           //
/////////////////////////////////////////////////////////////////////////////////

using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    internal static class NativeStructs
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

#pragma warning disable 0649
        [StructLayout(LayoutKind.Sequential)]
        internal struct RGNDATAHEADER
        {
            internal uint dwSize;
            internal uint iType;
            internal uint nCount;
            internal uint nRgnSize;
            internal RECT rcBound;
        };

#pragma warning restore 0649

        [StructLayout(LayoutKind.Sequential)]
        internal struct RGNDATA
        {
            internal RGNDATAHEADER rdh;

            internal unsafe static RECT* GetRectsPointer(RGNDATA* me)
            {
                return (RECT*)((byte*)me + sizeof(RGNDATAHEADER));
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RECT
        {
            internal int left;
            internal int top;
            internal int right;
            internal int bottom;
        }
    }
}
