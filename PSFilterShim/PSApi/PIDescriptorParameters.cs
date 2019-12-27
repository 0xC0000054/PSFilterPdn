/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2019 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

/* Adapted from PIActions.h
 * Copyright (c) 1996-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    internal struct PIReadDescriptor : IEquatable<PIReadDescriptor>
    {
        private readonly IntPtr value;

        public static readonly PIReadDescriptor Null = new PIReadDescriptor(0);

        public PIReadDescriptor(int index)
        {
            value = new IntPtr(index);
        }

        public int Index => value.ToInt32();

        public override bool Equals(object obj)
        {
            return obj is PIReadDescriptor other && Equals(other);
        }

        public bool Equals(PIReadDescriptor other)
        {
            return value == other.value;
        }

        public override int GetHashCode()
        {
            return -1584136870 + value.GetHashCode();
        }

        public static bool operator ==(PIReadDescriptor left, PIReadDescriptor right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PIReadDescriptor left, PIReadDescriptor right)
        {
            return !left.Equals(right);
        }
    }

    internal struct PIWriteDescriptor : IEquatable<PIWriteDescriptor>
    {
        private readonly IntPtr value;

        public static readonly PIWriteDescriptor Null = new PIWriteDescriptor(0);

        public PIWriteDescriptor(int index)
        {
            value = new IntPtr(index);
        }

        public int Index => value.ToInt32();

        public override bool Equals(object obj)
        {
            return obj is PIWriteDescriptor other && Equals(other);
        }

        public bool Equals(PIWriteDescriptor other)
        {
            return value == other.value;
        }

        public override int GetHashCode()
        {
            return -1584136870 + value.GetHashCode();
        }

        public static bool operator ==(PIWriteDescriptor left, PIWriteDescriptor right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(PIWriteDescriptor left, PIWriteDescriptor right)
        {
            return !left.Equals(right);
        }
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate PIWriteDescriptor OpenWriteDescriptorProc();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short CloseWriteDescriptorProc([In()] PIWriteDescriptor descriptor, [In(), Out()] ref IntPtr descriptorHandle);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutIntegerProc([In()] PIWriteDescriptor descriptor, [In()] uint key, [In()] int data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutFloatProc([In()] PIWriteDescriptor descriptor, [In()] uint key, [In()] ref double data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutUnitFloatProc([In()] PIWriteDescriptor descriptor, [In()] uint key, [In()] uint unit, [In()] ref double data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutBooleanProc([In()] PIWriteDescriptor descriptor, [In()] uint key, [In()] byte data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutTextProc([In()] PIWriteDescriptor descriptor, [In()] uint key, [In()] IntPtr data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutAliasProc([In()] PIWriteDescriptor descriptor, [In()] uint key, [In()] IntPtr data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutEnumeratedProc([In()] PIWriteDescriptor descriptor, [In()] uint key, [In()] uint type, [In()] uint data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutClassProc([In()] PIWriteDescriptor descriptor, [In()] uint key, [In()] uint data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutSimpleReferenceProc([In()] PIWriteDescriptor descriptor, [In()] uint key, [In()] ref PIDescriptorSimpleReference data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutObjectProc([In()] PIWriteDescriptor descriptor, [In()] uint key, [In()] uint type, [In()] IntPtr data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutCountProc([In()] PIWriteDescriptor descriptor, [In()] uint key, [In()] uint count);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutStringProc([In()] PIWriteDescriptor descriptor, [In()] uint key, [In()] IntPtr data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutScopedClassProc([In()] PIWriteDescriptor descriptor, [In()] uint key, [In()] uint type);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutScopedObjectProc([In()] PIWriteDescriptor descriptor, [In()] uint key, [In()] uint type, [In()] IntPtr data);

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct PIDescriptorSimpleReference__keyData
    {
        public fixed byte name[256];
        public int index;
        public uint type;
        public uint value;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PIDescriptorSimpleReference
    {
        public uint desiredClass;
        public uint keyForm;
        public PIDescriptorSimpleReference__keyData keyData;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate PIReadDescriptor OpenReadDescriptorProc([In()] IntPtr descriptor, [In()] IntPtr keyData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short CloseReadDescriptorProc([In()] PIReadDescriptor descriptor);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    internal delegate bool GetKeyProc([In()] PIReadDescriptor descriptor, [In(), Out()] ref uint key, [In(), Out()] ref uint type, [In(), Out()] ref int flags);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetIntegerProc([In()] PIReadDescriptor descriptor, [In(), Out()] ref int data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetFloatProc([In()] PIReadDescriptor descriptor, [In(), Out()] ref double data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetUnitFloatProc([In()] PIReadDescriptor descriptor, [In(), Out()] ref uint unit, [In(), Out()] ref double data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetBooleanProc([In()] PIReadDescriptor descriptor, [In(), Out()] ref byte data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetTextProc([In()] PIReadDescriptor descriptor, [In(), Out()] ref IntPtr data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetAliasProc([In()] PIReadDescriptor descriptor, [In(), Out()] ref IntPtr data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetEnumeratedProc([In()] PIReadDescriptor descriptor, [In(), Out()] ref uint data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetClassProc([In()] PIReadDescriptor descriptor, [In(), Out()] ref uint data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetSimpleReferenceProc([In()] PIReadDescriptor descriptor, [In(), Out()] ref PIDescriptorSimpleReference data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetObjectProc([In()] PIReadDescriptor descriptor, [In(), Out()] ref uint type, [In(), Out()] ref IntPtr data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetCountProc([In()] PIReadDescriptor descriptor, [In(), Out()] ref uint count);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetStringProc([In()] PIReadDescriptor descriptor, [In()] IntPtr data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetPinnedIntegerProc([In()] PIReadDescriptor descriptor, [In()] int min, [In()] int max, [In(), Out()] ref int data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetPinnedFloatProc([In()] PIReadDescriptor descriptor, [In()] ref double min, [In()] ref double max, [In(), Out()] ref double data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetPinnedUnitFloatProc([In()] PIReadDescriptor descriptor, [In()] ref double min, [In()] ref double max, [In()] ref uint unit, [In(), Out()] ref double data);

    [StructLayout(LayoutKind.Sequential)]
    internal struct WriteDescriptorProcs
    {
        public short writeDescriptorProcsVersion;
        public short numWriteDescriptorProcs;

        public IntPtr openWriteDescriptorProc;
        public IntPtr closeWriteDescriptorProc;
        public IntPtr putIntegerProc;
        public IntPtr putFloatProc;
        public IntPtr putUnitFloatProc;
        public IntPtr putBooleanProc;
        public IntPtr putTextProc;
        public IntPtr putAliasProc;
        public IntPtr putEnumeratedProc;
        public IntPtr putClassProc;
        public IntPtr putSimpleReferenceProc;
        public IntPtr putObjectProc;
        public IntPtr putCountProc;
        public IntPtr putStringProc;
        public IntPtr putScopedClassProc;
        public IntPtr putScopedObjectProc;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ReadDescriptorProcs
    {
        public short readDescriptorProcsVersion;
        public short numReadDescriptorProcs;

        public IntPtr openReadDescriptorProc;
        public IntPtr closeReadDescriptorProc;
        public IntPtr getKeyProc;
        public IntPtr getIntegerProc;
        public IntPtr getFloatProc;
        public IntPtr getUnitFloatProc;
        public IntPtr getBooleanProc;
        public IntPtr getTextProc;
        public IntPtr getAliasProc;
        public IntPtr getEnumeratedProc;
        public IntPtr getClassProc;
        public IntPtr getSimpleReferenceProc;
        public IntPtr getObjectProc;
        public IntPtr getCountProc;
        public IntPtr getStringProc;
        public IntPtr getPinnedIntegerProc;
        public IntPtr getPinnedFloatProc;
        public IntPtr getPinnedUnitFloatProc;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PIDescriptorParameters
    {
        public short descriptorParametersVersion;
        public PlayInfo playInfo;
        public RecordInfo recordInfo;
        public IntPtr descriptor;
        public IntPtr writeDescriptorProcs;
        public IntPtr readDescriptorProcs;
    }

    internal enum RecordInfo : short
    {
        plugInDialogOptional = 0,
        plugInDialogRequired = 1,
        plugInDialogNone = 2
    }

    internal enum PlayInfo : short
    {
        plugInDialogDontDisplay = 0,
        plugInDialogDisplay = 1,
        plugInDialogSilent = 2
    }
}
