/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2014 Nicholas Hayes
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
    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate IntPtr OpenWriteDescriptorProc();

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short CloseWriteDescriptorProc([In()] IntPtr descriptor, ref IntPtr descriptorHandle);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutIntegerProc([In()] IntPtr descriptor, [In()] uint key, [In()] int data);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutFloatProc([In()] IntPtr descriptor, [In()] uint key, [In()] ref double data);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutUnitFloatProc([In()] IntPtr descriptor, [In()] uint key, [In()] uint unit, [In()] ref double data);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutBooleanProc([In()] IntPtr descriptor, [In()] uint key, [In()] byte data);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutTextProc([In()] IntPtr descriptor, [In()] uint key, [In()] IntPtr data);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutAliasProc([In()] IntPtr descriptor, [In()] uint key, [In()] IntPtr data);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutEnumeratedProc([In()] IntPtr descriptor, [In()] uint key, [In()] uint type, [In()] uint data);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutClassProc([In()] IntPtr descriptor, [In()] uint key, [In()] uint data);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutSimpleReferenceProc([In()] IntPtr descriptor, [In()] uint key, [In()] ref PIDescriptorSimpleReference data);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutObjectProc([In()] IntPtr descriptor, [In()] uint key, [In()] uint type, [In()] IntPtr data);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutCountProc([In()] IntPtr descriptor, [In()] uint key, [In()] uint count);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutStringProc([In()] IntPtr descriptor, [In()] uint key, [In()] IntPtr data);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutScopedClassProc([In()] IntPtr descriptor, [In()] uint key, [In()] uint type);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short PutScopedObjectProc([In()] IntPtr descriptor, [In()] uint key, [In()] uint type, [In()] IntPtr data);

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
        public PIDescriptorSimpleReference__keyData Struct1;
    }

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate IntPtr OpenReadDescriptorProc([In()] IntPtr descriptor, [In()] IntPtr keyData);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short CloseReadDescriptorProc([In()] IntPtr descriptor);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate byte GetKeyProc([In()] IntPtr descriptor, ref uint key, ref uint type, ref int flags);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetIntegerProc([In()] IntPtr descriptor, ref int data);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetFloatProc([In()] IntPtr descriptor, ref double data);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetUnitFloatProc([In()] IntPtr descriptor, ref uint unit, ref double data);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetBooleanProc([In()] IntPtr descriptor, ref byte data);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetTextProc([In()] IntPtr descriptor, ref IntPtr data);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetAliasProc([In()] IntPtr descriptor, ref IntPtr data);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetEnumeratedProc([In()] IntPtr descriptor, ref uint data);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetClassProc([In()] IntPtr descriptor, ref uint data);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetSimpleReferenceProc([In()] IntPtr descriptor, ref PIDescriptorSimpleReference data);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetObjectProc([In()] IntPtr descriptor, ref uint type, ref IntPtr data);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetCountProc([In()] IntPtr descriptor, ref uint count);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetStringProc([In()] IntPtr descriptor, [In()] IntPtr data);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetPinnedIntegerProc([In()] IntPtr descriptor, [In()] int min, [In()] int max, ref int data);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetPinnedFloatProc([In()] IntPtr descriptor, [In()] ref double min, [In()] ref double max, ref double data);

    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short GetPinnedUnitFloatProc([In()] IntPtr descriptor, [In()] ref double min, [In()] ref double max, [In()] ref uint unit, ref double data);

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
