/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2016 Nicholas Hayes
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
    internal delegate int ActionDescriptorMake(ref IntPtr descriptor);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorFree(IntPtr descriptor);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorGetType(IntPtr descriptor, uint key, ref uint type);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorGetKey(IntPtr descriptor, uint index, ref uint key);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorHasKey(IntPtr descriptor, uint key, ref byte hasKey);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorGetCount(IntPtr descriptor, ref uint count);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorIsEqual(IntPtr descriptor, IntPtr other, ref byte isEqual);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorErase(IntPtr descriptor, uint key);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorClear(IntPtr descriptor);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutInteger(IntPtr descriptor, uint key, int value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutFloat(IntPtr descriptor, uint key, double value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutUnitFloat(IntPtr descriptor, uint key, uint unit, double value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutString(IntPtr descriptor, uint key, IntPtr cstrValue);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutBoolean(IntPtr descriptor, uint key, byte value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutList(IntPtr descriptor, uint key, IntPtr value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutObject(IntPtr descriptor, uint key, uint type, IntPtr value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutGlobalObject(IntPtr descriptor, uint key, uint type, IntPtr value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutEnumerated(IntPtr descriptor, uint key, uint type, uint value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutReference(IntPtr descriptor, uint key, IntPtr value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutClass(IntPtr descriptor, uint key, uint value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutGlobalClass(IntPtr descriptor, uint key, uint value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutAlias(IntPtr descriptor, uint key, IntPtr value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorGetInteger(IntPtr descriptor, uint key, ref int value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorGetFloat(IntPtr descriptor, uint key, ref double value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorGetUnitFloat(IntPtr descriptor, uint key, ref uint unit, ref double value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorGetStringLength(IntPtr descriptor, uint key, ref uint stringLength);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorGetString(IntPtr descriptor, uint key, IntPtr cstrValue, uint maxLength);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorGetBoolean(IntPtr descriptor, uint key, ref byte value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorGetList(IntPtr descriptor, uint key, ref IntPtr value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorGetObject(IntPtr descriptor, uint key, ref uint type, ref IntPtr value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorGetGlobalObject(IntPtr descriptor, uint key, ref uint type, ref IntPtr value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorGetEnumerated(IntPtr descriptor, uint key, ref uint type, ref uint value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorGetReference(IntPtr descriptor, uint key, ref IntPtr value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorGetClass(IntPtr descriptor, uint key, ref uint value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorGetGlobalClass(IntPtr descriptor, uint key, ref uint value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorGetAlias(IntPtr descriptor, uint key, ref IntPtr value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorHasKeys(IntPtr descriptor, IntPtr requiredKeys, ref byte hasKeys);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutIntegers(IntPtr descriptor, uint key, uint count, IntPtr value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorGetIntegers(IntPtr descriptor, uint key, uint count, IntPtr value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorAsHandle(IntPtr descriptor, ref IntPtr value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorHandleToDescriptor(IntPtr value, ref IntPtr descriptor);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutZString(IntPtr descriptor, uint key, IntPtr zstring);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorGetZString(IntPtr descriptor, uint key, ref IntPtr zstring);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutData(IntPtr descriptor, uint key, int length, IntPtr value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorGetDataLength(IntPtr descriptor, uint key, ref int value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorGetData(IntPtr descriptor, uint key, IntPtr value);

#pragma warning disable 108
    [StructLayout(LayoutKind.Sequential)]
    internal struct PSActionDescriptorProc
    {
        public IntPtr Make;
        public IntPtr Free;
        public IntPtr GetType;
        public IntPtr GetKey;
        public IntPtr HasKey;
        public IntPtr GetCount;
        public IntPtr IsEqual;
        public IntPtr Erase;
        public IntPtr Clear;
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
        public IntPtr HasKeys;
        public IntPtr PutIntegers;
        public IntPtr GetIntegers;
        public IntPtr AsHandle;
        public IntPtr HandleToDescriptor;
        public IntPtr PutZString;
        public IntPtr GetZString;
        public IntPtr PutData;
        public IntPtr GetDataLength;
        public IntPtr GetData;
    }
#pragma warning restore 108
}
