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

/* Adapted from PIActions.h
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi.PICA
{
    internal struct PIActionDescriptor : IEquatable<PIActionDescriptor>
    {
        private readonly IntPtr value;

        public static readonly PIActionDescriptor Null = new(0);

        public PIActionDescriptor(int index)
        {
            value = new IntPtr(index);
        }

        public readonly int Index => value.ToInt32();

        public override readonly bool Equals(object? obj)
        {
            return obj is PIActionDescriptor other && Equals(other);
        }

        public readonly bool Equals(PIActionDescriptor other)
        {
            return value == other.value;
        }

        public override readonly int GetHashCode()
        {
            return HashCode.Combine(value);
        }

        public override readonly string ToString()
        {
            return Index.ToString();
        }

        public static bool operator ==(PIActionDescriptor left, PIActionDescriptor right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PIActionDescriptor left, PIActionDescriptor right)
        {
            return !left.Equals(right);
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorMake(PIActionDescriptor* descriptor);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorFree(PIActionDescriptor descriptor);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetType(PIActionDescriptor descriptor, uint key, uint* type);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetKey(PIActionDescriptor descriptor, uint index, uint* key);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorHasKey(PIActionDescriptor descriptor, uint key, PSBoolean* hasKey);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetCount(PIActionDescriptor descriptor, uint* count);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorIsEqual(PIActionDescriptor descriptor, PIActionDescriptor other, PSBoolean* isEqual);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorErase(PIActionDescriptor descriptor, uint key);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorClear(PIActionDescriptor descriptor);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutInteger(PIActionDescriptor descriptor, uint key, int value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutFloat(PIActionDescriptor descriptor, uint key, double value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutUnitFloat(PIActionDescriptor descriptor, uint key, uint unit, double value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutString(PIActionDescriptor descriptor, uint key, IntPtr cstrValue);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutBoolean(PIActionDescriptor descriptor, uint key, byte value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutList(PIActionDescriptor descriptor, uint key, PIActionList value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutObject(PIActionDescriptor descriptor, uint key, uint type, PIActionDescriptor value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutGlobalObject(PIActionDescriptor descriptor, uint key, uint type, PIActionDescriptor value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutEnumerated(PIActionDescriptor descriptor, uint key, uint type, uint value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutReference(PIActionDescriptor descriptor, uint key, PIActionReference value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutClass(PIActionDescriptor descriptor, uint key, uint value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutGlobalClass(PIActionDescriptor descriptor, uint key, uint value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutAlias(PIActionDescriptor descriptor, uint key, Handle value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetInteger(PIActionDescriptor descriptor, uint key, int* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetFloat(PIActionDescriptor descriptor, uint key, double* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetUnitFloat(PIActionDescriptor descriptor, uint key, uint* unit, double* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetStringLength(PIActionDescriptor descriptor, uint key, uint* stringLength);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorGetString(PIActionDescriptor descriptor, uint key, IntPtr cstrValue, uint maxLength);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetBoolean(PIActionDescriptor descriptor, uint key, byte* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetList(PIActionDescriptor descriptor, uint key, PIActionList* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetObject(PIActionDescriptor descriptor, uint key, uint* type, PIActionDescriptor* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetGlobalObject(PIActionDescriptor descriptor, uint key, uint* type, PIActionDescriptor* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetEnumerated(PIActionDescriptor descriptor, uint key, uint* type, uint* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetReference(PIActionDescriptor descriptor, uint key, PIActionReference* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetClass(PIActionDescriptor descriptor, uint key, uint* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetGlobalClass(PIActionDescriptor descriptor, uint key, uint* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetAlias(PIActionDescriptor descriptor, uint key, Handle* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorHasKeys(PIActionDescriptor descriptor, IntPtr requiredKeys, PSBoolean* hasKeys);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutIntegers(PIActionDescriptor descriptor, uint key, uint count, IntPtr value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorGetIntegers(PIActionDescriptor descriptor, uint key, uint count, IntPtr value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorAsHandle(PIActionDescriptor descriptor, Handle* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorHandleToDescriptor(Handle value, PIActionDescriptor* descriptor);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutZString(PIActionDescriptor descriptor, uint key, ASZString zstring);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetZString(PIActionDescriptor descriptor, uint key, ASZString* zstring);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorPutData(PIActionDescriptor descriptor, uint key, int length, IntPtr value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionDescriptorGetDataLength(PIActionDescriptor descriptor, uint key, int* value);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionDescriptorGetData(PIActionDescriptor descriptor, uint key, IntPtr value);

#pragma warning disable 108
    [StructLayout(LayoutKind.Sequential)]
    internal struct PSActionDescriptorProc
    {
        public UnmanagedFunctionPointer<ActionDescriptorMake> Make;
        public UnmanagedFunctionPointer<ActionDescriptorFree> Free;
        public UnmanagedFunctionPointer<ActionDescriptorGetType> GetType;
        public UnmanagedFunctionPointer<ActionDescriptorGetKey> GetKey;
        public UnmanagedFunctionPointer<ActionDescriptorHasKey> HasKey;
        public UnmanagedFunctionPointer<ActionDescriptorGetCount> GetCount;
        public UnmanagedFunctionPointer<ActionDescriptorIsEqual> IsEqual;
        public UnmanagedFunctionPointer<ActionDescriptorErase> Erase;
        public UnmanagedFunctionPointer<ActionDescriptorClear> Clear;
        public UnmanagedFunctionPointer<ActionDescriptorPutInteger> PutInteger;
        public UnmanagedFunctionPointer<ActionDescriptorPutFloat> PutFloat;
        public UnmanagedFunctionPointer<ActionDescriptorPutUnitFloat> PutUnitFloat;
        public UnmanagedFunctionPointer<ActionDescriptorPutString> PutString;
        public UnmanagedFunctionPointer<ActionDescriptorPutBoolean> PutBoolean;
        public UnmanagedFunctionPointer<ActionDescriptorPutList> PutList;
        public UnmanagedFunctionPointer<ActionDescriptorPutObject> PutObject;
        public UnmanagedFunctionPointer<ActionDescriptorPutGlobalObject> PutGlobalObject;
        public UnmanagedFunctionPointer<ActionDescriptorPutEnumerated> PutEnumerated;
        public UnmanagedFunctionPointer<ActionDescriptorPutReference> PutReference;
        public UnmanagedFunctionPointer<ActionDescriptorPutClass> PutClass;
        public UnmanagedFunctionPointer<ActionDescriptorPutGlobalClass> PutGlobalClass;
        public UnmanagedFunctionPointer<ActionDescriptorPutAlias> PutAlias;
        public UnmanagedFunctionPointer<ActionDescriptorGetInteger> GetInteger;
        public UnmanagedFunctionPointer<ActionDescriptorGetFloat> GetFloat;
        public UnmanagedFunctionPointer<ActionDescriptorGetUnitFloat> GetUnitFloat;
        public UnmanagedFunctionPointer<ActionDescriptorGetStringLength> GetStringLength;
        public UnmanagedFunctionPointer<ActionDescriptorGetString> GetString;
        public UnmanagedFunctionPointer<ActionDescriptorGetBoolean> GetBoolean;
        public UnmanagedFunctionPointer<ActionDescriptorGetList> GetList;
        public UnmanagedFunctionPointer<ActionDescriptorGetObject> GetObject;
        public UnmanagedFunctionPointer<ActionDescriptorGetGlobalObject> GetGlobalObject;
        public UnmanagedFunctionPointer<ActionDescriptorGetEnumerated> GetEnumerated;
        public UnmanagedFunctionPointer<ActionDescriptorGetReference> GetReference;
        public UnmanagedFunctionPointer<ActionDescriptorGetClass> GetClass;
        public UnmanagedFunctionPointer<ActionDescriptorGetGlobalClass> GetGlobalClass;
        public UnmanagedFunctionPointer<ActionDescriptorGetAlias> GetAlias;
        public UnmanagedFunctionPointer<ActionDescriptorHasKeys> HasKeys;
        public UnmanagedFunctionPointer<ActionDescriptorPutIntegers> PutIntegers;
        public UnmanagedFunctionPointer<ActionDescriptorGetIntegers> GetIntegers;
        public UnmanagedFunctionPointer<ActionDescriptorAsHandle> AsHandle;
        public UnmanagedFunctionPointer<ActionDescriptorHandleToDescriptor> HandleToDescriptor;
        public UnmanagedFunctionPointer<ActionDescriptorPutZString> PutZString;
        public UnmanagedFunctionPointer<ActionDescriptorGetZString> GetZString;
        public UnmanagedFunctionPointer<ActionDescriptorPutData> PutData;
        public UnmanagedFunctionPointer<ActionDescriptorGetDataLength> GetDataLength;
        public UnmanagedFunctionPointer<ActionDescriptorGetData> GetData;
    }
#pragma warning restore 108
}
