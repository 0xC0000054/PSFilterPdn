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

        public static readonly PIReadDescriptor Null = new(0);

        public PIReadDescriptor(int index)
        {
            value = new IntPtr(index);
        }

        public readonly int Index => value.ToInt32();

        public override bool Equals(object obj)
        {
            return obj is PIReadDescriptor other && Equals(other);
        }

        public readonly bool Equals(PIReadDescriptor other)
        {
            return value == other.value;
        }

        public override readonly int GetHashCode()
        {
            return -1584136870 + value.GetHashCode();
        }

        public override string ToString()
        {
            return Index.ToString();
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

        public static readonly PIWriteDescriptor Null = new(0);

        public PIWriteDescriptor(int index)
        {
            value = new IntPtr(index);
        }

        public readonly int Index => value.ToInt32();

        public override bool Equals(object obj)
        {
            return obj is PIWriteDescriptor other && Equals(other);
        }

        public readonly bool Equals(PIWriteDescriptor other)
        {
            return value == other.value;
        }

        public override readonly int GetHashCode()
        {
            return -1584136870 + value.GetHashCode();
        }

        public override string ToString()
        {
            return Index.ToString();
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
    internal unsafe delegate short CloseWriteDescriptorProc([In()] PIWriteDescriptor descriptor, [In(), Out()] Handle* descriptorHandle);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutIntegerProc([In()] PIWriteDescriptor descriptor, [In()] uint key, [In()] int data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate short PutFloatProc([In()] PIWriteDescriptor descriptor, [In()] uint key, [In()] double* data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate short PutUnitFloatProc([In()] PIWriteDescriptor descriptor, [In()] uint key, [In()] uint unit, [In()] double* data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutBooleanProc([In()] PIWriteDescriptor descriptor, [In()] uint key, [In()] byte data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutTextProc([In()] PIWriteDescriptor descriptor, [In()] uint key, [In()] Handle data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutAliasProc([In()] PIWriteDescriptor descriptor, [In()] uint key, [In()] Handle data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutEnumeratedProc([In()] PIWriteDescriptor descriptor, [In()] uint key, [In()] uint type, [In()] uint data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutClassProc([In()] PIWriteDescriptor descriptor, [In()] uint key, [In()] uint data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate short PutSimpleReferenceProc([In()] PIWriteDescriptor descriptor, [In()] uint key, [In()] PIDescriptorSimpleReference* data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutObjectProc([In()] PIWriteDescriptor descriptor, [In()] uint key, [In()] uint type, [In()] Handle data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutCountProc([In()] PIWriteDescriptor descriptor, [In()] uint key, [In()] uint count);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutStringProc([In()] PIWriteDescriptor descriptor, [In()] uint key, [In()] IntPtr data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutScopedClassProc([In()] PIWriteDescriptor descriptor, [In()] uint key, [In()] uint type);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutScopedObjectProc([In()] PIWriteDescriptor descriptor, [In()] uint key, [In()] uint type, [In()] Handle data);

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
    internal delegate PIReadDescriptor OpenReadDescriptorProc([In()] Handle descriptor, [In()] IntPtr keyData);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short CloseReadDescriptorProc([In()] PIReadDescriptor descriptor);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate PSBoolean GetKeyProc([In()] PIReadDescriptor descriptor, [In(), Out()] uint* key, [In(), Out()] uint* type, [In(), Out()] int* flags);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate short GetIntegerProc([In()] PIReadDescriptor descriptor, [In(), Out()] int* data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate short GetFloatProc([In()] PIReadDescriptor descriptor, [In(), Out()] double* data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate short GetUnitFloatProc([In()] PIReadDescriptor descriptor, [In(), Out()] uint* unit, [In(), Out()] double* data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate short GetBooleanProc([In()] PIReadDescriptor descriptor, [In(), Out()] byte* data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate short GetTextProc([In()] PIReadDescriptor descriptor, [In(), Out()] Handle* data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate short GetAliasProc([In()] PIReadDescriptor descriptor, [In(), Out()] Handle* data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate short GetEnumeratedProc([In()] PIReadDescriptor descriptor, [In(), Out()] uint* data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate short GetClassProc([In()] PIReadDescriptor descriptor, [In(), Out()] uint* data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate short GetSimpleReferenceProc([In()] PIReadDescriptor descriptor, [In(), Out()] PIDescriptorSimpleReference* data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate short GetObjectProc([In()] PIReadDescriptor descriptor, [In(), Out()] uint* type, [In(), Out()] Handle* data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate short GetCountProc([In()] PIReadDescriptor descriptor, [In(), Out()] uint* count);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetStringProc([In()] PIReadDescriptor descriptor, [In()] IntPtr data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate short GetPinnedIntegerProc([In()] PIReadDescriptor descriptor, [In()] int min, [In()] int max, [In(), Out()] int* data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate short GetPinnedFloatProc([In()] PIReadDescriptor descriptor, [In()] double* min, [In()] double* max, [In(), Out()] double* data);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate short GetPinnedUnitFloatProc([In()] PIReadDescriptor descriptor, [In()] double* min, [In()] double* max, [In()] uint* unit, [In(), Out()] double* data);

    [StructLayout(LayoutKind.Sequential)]
    internal struct WriteDescriptorProcs
    {
        public short writeDescriptorProcsVersion;
        public short numWriteDescriptorProcs;

        public UnmanagedFunctionPointer<OpenWriteDescriptorProc> openWriteDescriptorProc;
        public UnmanagedFunctionPointer<CloseWriteDescriptorProc> closeWriteDescriptorProc;
        public UnmanagedFunctionPointer<PutIntegerProc> putIntegerProc;
        public UnmanagedFunctionPointer<PutFloatProc> putFloatProc;
        public UnmanagedFunctionPointer<PutUnitFloatProc> putUnitFloatProc;
        public UnmanagedFunctionPointer<PutBooleanProc> putBooleanProc;
        public UnmanagedFunctionPointer<PutTextProc> putTextProc;
        public UnmanagedFunctionPointer<PutAliasProc> putAliasProc;
        public UnmanagedFunctionPointer<PutEnumeratedProc> putEnumeratedProc;
        public UnmanagedFunctionPointer<PutClassProc> putClassProc;
        public UnmanagedFunctionPointer<PutSimpleReferenceProc> putSimpleReferenceProc;
        public UnmanagedFunctionPointer<PutObjectProc> putObjectProc;
        public UnmanagedFunctionPointer<PutCountProc> putCountProc;
        public UnmanagedFunctionPointer<PutStringProc> putStringProc;
        public UnmanagedFunctionPointer<PutScopedClassProc> putScopedClassProc;
        public UnmanagedFunctionPointer<PutScopedObjectProc> putScopedObjectProc;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ReadDescriptorProcs
    {
        public short readDescriptorProcsVersion;
        public short numReadDescriptorProcs;

        public UnmanagedFunctionPointer<OpenReadDescriptorProc> openReadDescriptorProc;
        public UnmanagedFunctionPointer<CloseReadDescriptorProc> closeReadDescriptorProc;
        public UnmanagedFunctionPointer<GetKeyProc> getKeyProc;
        public UnmanagedFunctionPointer<GetIntegerProc> getIntegerProc;
        public UnmanagedFunctionPointer<GetFloatProc> getFloatProc;
        public UnmanagedFunctionPointer<GetUnitFloatProc> getUnitFloatProc;
        public UnmanagedFunctionPointer<GetBooleanProc> getBooleanProc;
        public UnmanagedFunctionPointer<GetTextProc> getTextProc;
        public UnmanagedFunctionPointer<GetAliasProc> getAliasProc;
        public UnmanagedFunctionPointer<GetEnumeratedProc> getEnumeratedProc;
        public UnmanagedFunctionPointer<GetClassProc> getClassProc;
        public UnmanagedFunctionPointer<GetSimpleReferenceProc> getSimpleReferenceProc;
        public UnmanagedFunctionPointer<GetObjectProc> getObjectProc;
        public UnmanagedFunctionPointer<GetCountProc> getCountProc;
        public UnmanagedFunctionPointer<GetStringProc> getStringProc;
        public UnmanagedFunctionPointer<GetPinnedIntegerProc> getPinnedIntegerProc;
        public UnmanagedFunctionPointer<GetPinnedFloatProc> getPinnedFloatProc;
        public UnmanagedFunctionPointer<GetPinnedUnitFloatProc> getPinnedUnitFloatProc;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PIDescriptorParameters
    {
        public short descriptorParametersVersion;
        public PlayInfo playInfo;
        public RecordInfo recordInfo;
        public Handle descriptor;
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
