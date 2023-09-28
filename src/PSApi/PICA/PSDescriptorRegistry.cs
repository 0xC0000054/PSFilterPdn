/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2023 Nicholas Hayes
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
    internal delegate int DescriptorRegistryRegister(IntPtr key, PIActionDescriptor descriptor, PSBoolean isPersistent);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int DescriptorRegistryErase(IntPtr key);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int DescriptorRegistryGet(IntPtr key, PIActionDescriptor* descriptor);

    internal struct PSDescriptorRegistryProcs
    {
        public UnmanagedFunctionPointer<DescriptorRegistryRegister> Register;
        public UnmanagedFunctionPointer<DescriptorRegistryErase> Erase;
        public UnmanagedFunctionPointer<DescriptorRegistryGet> Get;
    }
}
