/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2020 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

/* Adapted from PIActions.h
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi.PICA
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int DescriptorRegistryRegister(IntPtr key, PIActionDescriptor descriptor, [MarshalAs(UnmanagedType.U1)] bool isPersistent);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int DescriptorRegistryErase(IntPtr key);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int DescriptorRegistryGet(IntPtr key, ref PIActionDescriptor descriptor);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable")]
    internal struct PSDescriptorRegistryProcs
    {
        public IntPtr Register;
        public IntPtr Erase;
        public IntPtr Get;
    }
}
