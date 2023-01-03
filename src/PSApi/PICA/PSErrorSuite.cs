/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2023 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

/* Adapted from PIErrorSuite.h
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi.PICA
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ErrorSuiteSetErrorFromPString(IntPtr str);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ErrorSuiteSetErrorFromCString(IntPtr str);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ErrorSuiteSetErrorFromZString(ASZString str);

    [StructLayout(LayoutKind.Sequential)]
    internal struct PSErrorSuite1
    {
        public UnmanagedFunctionPointer<ErrorSuiteSetErrorFromPString> SetErrorFromPString;
        public UnmanagedFunctionPointer<ErrorSuiteSetErrorFromCString> SetErrorFromCString;
        public UnmanagedFunctionPointer<ErrorSuiteSetErrorFromZString> SetErrorFromZString;
    }
}
