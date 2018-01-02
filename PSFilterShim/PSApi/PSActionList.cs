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

namespace PSFilterLoad.PSApi
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListMake(ref IntPtr actionList);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListFree(IntPtr actionList);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListGetType(IntPtr list, uint index, ref uint value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListGetCount(IntPtr list, ref uint value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListPutInteger(IntPtr list, int value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListPutFloat(IntPtr list, double value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListPutUnitFloat(IntPtr list, uint unit, double value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListPutString(IntPtr list, IntPtr cstr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListPutBoolean(IntPtr list, byte value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListPutList(IntPtr list, IntPtr value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListPutObject(IntPtr list, uint type, IntPtr descriptor);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListPutGlobalObject(IntPtr list, uint type, IntPtr descriptor);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListPutEnumerated(IntPtr list, uint type, uint value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListPutReference(IntPtr list, IntPtr value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListPutClass(IntPtr list, uint value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListPutGlobalClass(IntPtr list, uint value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListPutAlias(IntPtr list, IntPtr value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListGetInteger(IntPtr list, uint index, ref int value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListGetFloat(IntPtr list, uint index, ref double value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListGetUnitFloat(IntPtr list, uint index, ref uint unit, ref double value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListGetStringLength(IntPtr list, uint index, ref uint stringLength);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListGetString(IntPtr list, uint index, IntPtr cstr, uint maxLength);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListGetBoolean(IntPtr list, uint index, ref byte value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListGetList(IntPtr list, uint index, ref IntPtr value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListGetObject(IntPtr list, uint index, ref uint type, ref IntPtr descriptor);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListGetGlobalObject(IntPtr list, uint index, ref uint type, ref IntPtr descriptor);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListGetEnumerated(IntPtr list, uint index, ref uint type, ref uint value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListGetReference(IntPtr list, uint index, ref IntPtr value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListGetClass(IntPtr list, uint index, ref uint value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListGetGlobalClass(IntPtr list, uint index, ref uint value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListGetAlias(IntPtr list, uint index, ref IntPtr aliasHandle);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListPutIntegers(IntPtr list, uint count, IntPtr value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListGetIntegers(IntPtr list, uint count, IntPtr value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListPutData(IntPtr list, int length, IntPtr data);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListGetDataLength(IntPtr list, uint index, ref int length);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListGetData(IntPtr list, uint index, IntPtr value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListPutZString(IntPtr list, IntPtr zstring);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListGetZString(IntPtr list, uint index, ref IntPtr zstring);

#pragma warning disable 108
    [StructLayout(LayoutKind.Sequential)]
    internal struct PSActionListProcs
    {
        public IntPtr Make;
        public IntPtr Free;
        public IntPtr GetType;
        public IntPtr GetCount;
        public IntPtr PutInteger;
        public IntPtr PutFloat;
        public IntPtr PutUnitFloat;
        public IntPtr PutString;
        public IntPtr PutBoolean;
        public IntPtr PutList;
        public IntPtr PutObject;
        public IntPtr PutGlobalObject;
        public IntPtr PutEnumerated;
        public IntPtr PutReference;
        public IntPtr PutClass;
        public IntPtr PutGlobalClass;
        public IntPtr PutAlias;
        public IntPtr GetInteger;
        public IntPtr GetFloat;
        public IntPtr GetUnitFloat;
        public IntPtr GetStringLength;
        public IntPtr GetString;
        public IntPtr GetBoolean;
        public IntPtr GetList;
        public IntPtr GetObject;
        public IntPtr GetGlobalObject;
        public IntPtr GetEnumerated;
        public IntPtr GetReference;
        public IntPtr GetClass;
        public IntPtr GetGlobalClass;
        public IntPtr GetAlias;
        public IntPtr PutIntegers;
        public IntPtr GetIntegers;
        public IntPtr PutData;
        public IntPtr GetDataLength;
        public IntPtr GetData;
        public IntPtr PutZString;
        public IntPtr GetZString;
    }
#pragma warning restore 108
}
