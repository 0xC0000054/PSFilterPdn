/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2018 Nicholas Hayes
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
    internal delegate int ActionReferenceMake(ref IntPtr reference);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionReferenceFree(IntPtr reference);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionReferenceGetForm(IntPtr reference, ref uint value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionReferenceGetDesiredClass(IntPtr reference, ref uint value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionReferencePutName(IntPtr reference, uint desiredClass, IntPtr cstrValue);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionReferencePutIndex(IntPtr reference, uint desiredClass, uint value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionReferencePutIdentifier(IntPtr reference, uint desiredClass, uint value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionReferencePutOffset(IntPtr reference, uint desiredClass, int value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionReferencePutEnumerated(IntPtr reference, uint desiredClass, uint type, uint value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionReferencePutProperty(IntPtr reference, uint desiredClass, uint value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionReferencePutClass(IntPtr reference, uint desiredClass);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionReferenceGetNameLength(IntPtr reference, ref uint stringLength);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionReferenceGetName(IntPtr reference, IntPtr name, uint maxLength);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionReferenceGetIndex(IntPtr reference, ref uint value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionReferenceGetIdentifier(IntPtr reference, ref uint value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionReferenceGetOffset(IntPtr reference, ref int value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionReferenceGetEnumerated(IntPtr reference, ref uint type, ref uint enumValue);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionReferenceGetProperty(IntPtr reference, ref uint value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionReferenceGetContainer(IntPtr reference, ref IntPtr value);

    [StructLayout(LayoutKind.Sequential)]
    internal struct PSActionReferenceProcs
    {
        public IntPtr Make;
        public IntPtr Free;
        public IntPtr GetForm;
        public IntPtr GetDesiredClass;
        public IntPtr PutName;
        public IntPtr PutIndex;
        public IntPtr PutIdentifier;
        public IntPtr PutOffset;
        public IntPtr PutEnumerated;
        public IntPtr PutProperty;
        public IntPtr PutClass;
        public IntPtr GetNameLength;
        public IntPtr GetName;
        public IntPtr GetIndex;
        public IntPtr GetIdentifier;
        public IntPtr GetOffset;
        public IntPtr GetEnumerated;
        public IntPtr GetProperty;
        public IntPtr GetContainer;
    }
}
