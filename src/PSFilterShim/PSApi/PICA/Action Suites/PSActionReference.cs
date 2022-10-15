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
    internal struct PIActionReference : IEquatable<PIActionReference>
    {
        private readonly IntPtr value;

        public static readonly PIActionReference Null = new(0);

        public PIActionReference(int index)
        {
            value = new IntPtr(index);
        }

        public int Index => value.ToInt32();

        public override bool Equals(object obj)
        {
            return obj is PIActionReference other && Equals(other);
        }

        public bool Equals(PIActionReference other)
        {
            return value == other.value;
        }

        public override int GetHashCode()
        {
            return -1584136870 + value.GetHashCode();
        }

        public static bool operator ==(PIActionReference left, PIActionReference right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PIActionReference left, PIActionReference right)
        {
            return !left.Equals(right);
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionReferenceMake(PIActionReference* reference);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionReferenceFree(PIActionReference reference);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionReferenceGetForm(PIActionReference reference, uint* value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionReferenceGetDesiredClass(PIActionReference reference, uint* value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionReferencePutName(PIActionReference reference, uint desiredClass, IntPtr cstrValue);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionReferencePutIndex(PIActionReference reference, uint desiredClass, uint value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionReferencePutIdentifier(PIActionReference reference, uint desiredClass, uint value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionReferencePutOffset(PIActionReference reference, uint desiredClass, int value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionReferencePutEnumerated(PIActionReference reference, uint desiredClass, uint type, uint value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionReferencePutProperty(PIActionReference reference, uint desiredClass, uint value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionReferencePutClass(PIActionReference reference, uint desiredClass);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionReferenceGetNameLength(PIActionReference reference, uint* stringLength);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ActionReferenceGetName(PIActionReference reference, IntPtr name, uint maxLength);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionReferenceGetIndex(PIActionReference reference, uint* value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionReferenceGetIdentifier(PIActionReference reference, uint* value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionReferenceGetOffset(PIActionReference reference, int* value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionReferenceGetEnumerated(PIActionReference reference, uint* type, uint* enumValue);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionReferenceGetProperty(PIActionReference reference, uint* value);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate int ActionReferenceGetContainer(PIActionReference reference, PIActionReference* value);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable")]
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
