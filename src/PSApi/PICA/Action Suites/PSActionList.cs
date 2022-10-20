/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2022 Nicholas Hayes
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
    internal struct PIActionList : IEquatable<PIActionList>
    {
        private readonly IntPtr value;

        public PIActionList(int index)
        {
            value = new IntPtr(index);
        }

        public int Index => value.ToInt32();

        public override bool Equals(object obj)
        {
            return obj is PIActionList other && Equals(other);
        }

        public bool Equals(PIActionList other)
        {
            return value == other.value;
        }

        public override int GetHashCode()
        {
            return -1584136870 + value.GetHashCode();
        }

        public override string ToString()
        {
            return Index.ToString();
        }

        public static bool operator ==(PIActionList left, PIActionList right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PIActionList left, PIActionList right)
        {
            return !left.Equals(right);
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionListMake(PIActionList* actionList);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListFree(PIActionList actionList);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionListGetType(PIActionList list, uint index, uint* value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionListGetCount(PIActionList list, uint* value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListPutInteger(PIActionList list, int value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListPutFloat(PIActionList list, double value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListPutUnitFloat(PIActionList list, uint unit, double value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListPutString(PIActionList list, IntPtr cstr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListPutBoolean(PIActionList list, byte value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListPutList(PIActionList list, PIActionList value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListPutObject(PIActionList list, uint type, PIActionDescriptor descriptor);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListPutGlobalObject(PIActionList list, uint type, PIActionDescriptor descriptor);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListPutEnumerated(PIActionList list, uint type, uint value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListPutReference(PIActionList list, PIActionReference value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListPutClass(PIActionList list, uint value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListPutGlobalClass(PIActionList list, uint value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListPutAlias(PIActionList list, Handle value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionListGetInteger(PIActionList list, uint index, int* value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionListGetFloat(PIActionList list, uint index, double* value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionListGetUnitFloat(PIActionList list, uint index, uint* unit, double* value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionListGetStringLength(PIActionList list, uint index, uint* stringLength);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListGetString(PIActionList list, uint index, IntPtr cstr, uint maxLength);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionListGetBoolean(PIActionList list, uint index, byte* value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionListGetList(PIActionList list, uint index, PIActionList* value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionListGetObject(PIActionList list, uint index, uint* type, PIActionDescriptor* descriptor);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionListGetGlobalObject(PIActionList list, uint index, uint* type, PIActionDescriptor* descriptor);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionListGetEnumerated(PIActionList list, uint index, uint* type, uint* value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionListGetReference(PIActionList list, uint index, PIActionReference* value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionListGetClass(PIActionList list, uint index, uint* value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionListGetGlobalClass(PIActionList list, uint index, uint* value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionListGetAlias(PIActionList list, uint index, Handle* aliasHandle);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListPutIntegers(PIActionList list, uint count, IntPtr value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListGetIntegers(PIActionList list, uint count, IntPtr value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListPutData(PIActionList list, int length, IntPtr data);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionListGetDataLength(PIActionList list, uint index, int* length);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListGetData(PIActionList list, uint index, IntPtr value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionListPutZString(PIActionList list, ASZString zstring);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionListGetZString(PIActionList list, uint index, ASZString* zstring);

#pragma warning disable 108
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable")]
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
