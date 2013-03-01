/* Adapted from PIActions.h
 * Copyright (c) 1996, Adobe Systems Incorporated.
 * All rights reserved.
*/


using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{


    /// Return Type: PIWriteDescriptor->PIOpaqueWriteDescriptor*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate System.IntPtr OpenWriteDescriptorProc();

    /// Return Type: OSErr->short
    ///param0: PIWriteDescriptor->PIOpaqueWriteDescriptor*
    ///param1: PIDescriptorHandle*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short CloseWriteDescriptorProc(System.IntPtr descriptor, ref System.IntPtr descriptorHandle);

    /// Return Type: OSErr->short
    ///descriptor: PIWriteDescriptor->PIOpaqueWriteDescriptor*
    ///key: DescriptorKeyID->unsigned int
    ///param2: int32->int
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutIntegerProc([In()]System.IntPtr descriptor, [In()]uint key, [In()]int data);

    /// Return Type: OSErr->short
    ///descriptor: PIWriteDescriptor->PIOpaqueWriteDescriptor*
    ///key: DescriptorKeyID->unsigned int
    ///param2: double*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutFloatProc(System.IntPtr descriptor, uint key, ref double data);

    /// Return Type: OSErr->short
    ///descriptor: PIWriteDescriptor->PIOpaqueWriteDescriptor*
    ///key: DescriptorKeyID->unsigned int
    ///param2: DescriptorUnitID->unsigned int
    ///param3: double*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutUnitFloatProc([In()]System.IntPtr descriptor, uint key, uint type, ref double data);

    /// Return Type: OSErr->short
    ///descriptor: PIWriteDescriptor->PIOpaqueWriteDescriptor*
    ///key: DescriptorKeyID->unsigned int
    ///param2: Boolean->BYTE->unsigned char
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutBooleanProc([In()]System.IntPtr descriptor, uint key, [In()]byte data);

    /// Return Type: OSErr->short
    ///descriptor: PIWriteDescriptor->PIOpaqueWriteDescriptor*
    ///key: DescriptorKeyID->unsigned int
    ///param2: Handle->LPSTR*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutTextProc([In()]System.IntPtr descriptor, [In()]uint key, [In()]IntPtr handle);

    /// Return Type: OSErr->short
    ///descriptor: PIWriteDescriptor->PIOpaqueWriteDescriptor*
    ///key: DescriptorKeyID->unsigned int
    ///param2: Handle->LPSTR*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutAliasProc(System.IntPtr descriptor, uint key, [In()]System.IntPtr aliasHandle);

    /// Return Type: OSErr->short
    ///descriptor: PIWriteDescriptor->PIOpaqueWriteDescriptor*
    ///key: DescriptorKeyID->unsigned int
    ///type: DescriptorTypeID->unsigned int
    ///value: DescriptorEnumID->unsigned int
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutEnumeratedProc(System.IntPtr descriptor, uint key, uint type, uint value);

    /// Return Type: OSErr->short
    ///descriptor: PIWriteDescriptor->PIOpaqueWriteDescriptor*
    ///key: DescriptorKeyID->unsigned int
    ///param2: DescriptorTypeID->unsigned int
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutClassProc(System.IntPtr descriptor, uint key, uint type);

    /// Return Type: OSErr->short
    ///descriptor: PIWriteDescriptor->PIOpaqueWriteDescriptor*
    ///key: DescriptorKeyID->unsigned int
    ///param2: PIDescriptorSimpleReference*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutSimpleReferenceProc(System.IntPtr descriptor, uint key, ref PIDescriptorSimpleReference data);

    /// Return Type: OSErr->short
    ///descriptor: PIWriteDescriptor->PIOpaqueWriteDescriptor*
    ///key: DescriptorKeyID->unsigned int
    ///param2: DescriptorTypeID->unsigned int
    ///param3: PIDescriptorHandle->Handle->LPSTR*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutObjectProc(System.IntPtr descriptor, uint key, uint type, System.IntPtr handle);

    /// Return Type: OSErr->short
    ///descriptor: PIWriteDescriptor->PIOpaqueWriteDescriptor*
    ///key: DescriptorKeyID->unsigned int
    ///count: uint32->unsigned int
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutCountProc(System.IntPtr descriptor, uint key, uint count);

    /// Return Type: OSErr->short
    ///descriptor: PIWriteDescriptor->PIOpaqueWriteDescriptor*
    ///key: DescriptorKeyID->unsigned int
    ///param2: ConstStr255Param->char*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutStringProc(System.IntPtr descriptor, uint key, IntPtr handle);

    /// Return Type: OSErr->short
    ///descriptor: PIWriteDescriptor->PIOpaqueWriteDescriptor*
    ///key: DescriptorKeyID->unsigned int
    ///param2: DescriptorTypeID->unsigned int
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutScopedClassProc(System.IntPtr descriptor, uint key, uint type);

    /// Return Type: OSErr->short
    ///descriptor: PIWriteDescriptor->PIOpaqueWriteDescriptor*
    ///key: DescriptorKeyID->unsigned int
    ///param2: DescriptorTypeID->unsigned int
    ///param3: PIDescriptorHandle->Handle->LPSTR*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutScopedObjectProc(System.IntPtr descriptor, uint key, uint type, ref System.IntPtr handle);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    internal unsafe struct PIDescriptorSimpleReference__keyData
    {

        /// Str255->unsigned char[256]
        public fixed byte name[256];

        /// int32->int
        public int index;

        /// DescriptorTypeID->unsigned int
        public uint type;

        /// DescriptorEnumID->unsigned int
        public uint value;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct PIDescriptorSimpleReference
    {

        /// DescriptorTypeID->unsigned int
        public uint desiredClass;

        /// DescriptorKeyID->unsigned int
        public uint keyForm;

        /// PIDescriptorSimpleReference__keyData
        public PIDescriptorSimpleReference__keyData Struct1;
    }

    /// Return Type: PIReadDescriptor->PIOpaqueReadDescriptor*
    ///descriptor: PIDescriptorHandle->Handle->LPSTR*
    ///data: DescriptorKeyIDArray->DescriptorKeyID[]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate System.IntPtr OpenReadDescriptorProc(System.IntPtr descriptorHandle, IntPtr keyArray);

    /// Return Type: OSErr->short
    ///descriptor: PIReadDescriptor->PIOpaqueReadDescriptor*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short CloseReadDescriptorProc(System.IntPtr descriptor);

    /// Return Type: Boolean->BYTE->unsigned char
    ///descriptor: PIReadDescriptor->PIOpaqueReadDescriptor*
    ///key: DescriptorKeyID*
    ///type: DescriptorTypeID*
    ///flags: int32*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate byte GetKeyProc(System.IntPtr descriptor, ref uint key, ref uint type, ref int flags);

    /// Return Type: OSErr->short
    ///descriptor: PIReadDescriptor->PIOpaqueReadDescriptor*
    ///data: int32*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetIntegerProc(System.IntPtr descriptor, ref int data);

    /// Return Type: OSErr->short
    ///descriptor: PIReadDescriptor->PIOpaqueReadDescriptor*
    ///data: double*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetFloatProc(System.IntPtr descriptor, ref double data);

    /// Return Type: OSErr->short
    ///descriptor: PIReadDescriptor->PIOpaqueReadDescriptor*
    ///data: DescriptorUnitID*
    ///param2: double*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetUnitFloatProc(System.IntPtr descriptor, ref uint unit, ref double data);

    /// Return Type: OSErr->short
    ///descriptor: PIReadDescriptor->PIOpaqueReadDescriptor*
    ///data: Boolean*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetBooleanProc(System.IntPtr descriptor, ref byte data);

    /// Return Type: OSErr->short
    ///descriptor: PIReadDescriptor->PIOpaqueReadDescriptor*
    ///data: Handle*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetTextProc(System.IntPtr descriptor, ref System.IntPtr data);

    /// Return Type: OSErr->short
    ///descriptor: PIReadDescriptor->PIOpaqueReadDescriptor*
    ///data: Handle*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetAliasProc(System.IntPtr descriptor, ref System.IntPtr data);

    /// Return Type: OSErr->short
    ///descriptor: PIReadDescriptor->PIOpaqueReadDescriptor*
    ///data: DescriptorEnumID*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetEnumeratedProc(System.IntPtr descriptor, ref uint data);

    /// Return Type: OSErr->short
    ///descriptor: PIReadDescriptor->PIOpaqueReadDescriptor*
    ///data: DescriptorTypeID*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetClassProc(System.IntPtr descriptor, ref uint data);

    /// Return Type: OSErr->short
    ///descriptor: PIReadDescriptor->PIOpaqueReadDescriptor*
    ///data: PIDescriptorSimpleReference*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetSimpleReferenceProc(System.IntPtr descriptor, ref PIDescriptorSimpleReference data);

    /// Return Type: OSErr->short
    ///descriptor: PIReadDescriptor->PIOpaqueReadDescriptor*
    ///data: DescriptorTypeID*
    ///param2: PIDescriptorHandle*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetObjectProc(System.IntPtr descriptor, ref uint type, ref System.IntPtr data);

    /// Return Type: OSErr->short
    ///descriptor: PIReadDescriptor->PIOpaqueReadDescriptor*
    ///data: uint32*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetCountProc(System.IntPtr descriptor, ref uint data);

    /// Return Type: OSErr->short
    ///descriptor: PIReadDescriptor->PIOpaqueReadDescriptor*
    ///data: Str255*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetStringProc(System.IntPtr descriptor, System.IntPtr data);

    /// Return Type: OSErr->short
    ///descriptor: PIReadDescriptor->PIOpaqueReadDescriptor*
    ///data: int32->int
    ///param2: int32->int
    ///param3: int32*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetPinnedIntegerProc(System.IntPtr descriptor, int min, int max, ref int data);

    /// Return Type: OSErr->short
    ///descriptor: PIReadDescriptor->PIOpaqueReadDescriptor*
    ///data: double*
    ///param2: double*
    ///param3: double*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetPinnedFloatProc(System.IntPtr descriptor, ref double max, ref double min, ref double data);

    /// Return Type: OSErr->short
    ///descriptor: PIReadDescriptor->PIOpaqueReadDescriptor*
    ///data: double*
    ///param2: double*
    ///param3: DescriptorUnitID*
    ///param4: double*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetPinnedUnitFloatProc(System.IntPtr descriptor, ref double min, ref double max, ref uint unit, ref double data);

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable"), System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct WriteDescriptorProcs
    {

        /// int16->short
        public short writeDescriptorProcsVersion;

        /// int16->short
        public short numWriteDescriptorProcs;

        /// OpenWriteDescriptorProc
        public IntPtr openWriteDescriptorProc;

        /// CloseWriteDescriptorProc
        public IntPtr closeWriteDescriptorProc;

        /// PutIntegerProc
        public IntPtr putIntegerProc;

        /// PutFloatProc
        public IntPtr putFloatProc;

        /// PutUnitFloatProc
        public IntPtr putUnitFloatProc;

        /// PutBooleanProc
        public IntPtr putBooleanProc;

        /// PutTextProc
        public IntPtr putTextProc;

        /// PutAliasProc
        public IntPtr putAliasProc;

        /// PutEnumeratedProc
        public IntPtr putEnumeratedProc;

        /// PutClassProc
        public IntPtr putClassProc;

        /// PutSimpleReferenceProc
        public IntPtr putSimpleReferenceProc;

        /// PutObjectProc
        public IntPtr putObjectProc;

        /// PutCountProc
        public IntPtr putCountProc;

        /// PutStringProc
        public IntPtr putStringProc;

        /// PutScopedClassProc
        public IntPtr putScopedClassProc;

        /// PutScopedObjectProc
        public IntPtr putScopedObjectProc;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable"), System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct ReadDescriptorProcs
    {

        /// int16->short
        public short readDescriptorProcsVersion;

        /// int16->short
        public short numReadDescriptorProcs;

        /// OpenReadDescriptorProc
        public IntPtr openReadDescriptorProc;

        /// CloseReadDescriptorProc
        public IntPtr closeReadDescriptorProc;

        /// GetKeyProc
        public IntPtr getKeyProc;

        /// GetIntegerProc
        public IntPtr getIntegerProc;

        /// GetFloatProc
        public IntPtr getFloatProc;

        /// GetUnitFloatProc
        public IntPtr getUnitFloatProc;

        /// GetBooleanProc
        public IntPtr getBooleanProc;

        /// GetTextProc
        public IntPtr getTextProc;

        /// GetAliasProc
        public IntPtr getAliasProc;

        /// GetEnumeratedProc
        public IntPtr getEnumeratedProc;

        /// GetClassProc
        public IntPtr getClassProc;

        /// GetSimpleReferenceProc
        public IntPtr getSimpleReferenceProc;

        /// GetObjectProc
        public IntPtr getObjectProc;

        /// GetCountProc
        public IntPtr getCountProc;

        /// GetStringProc
        public IntPtr getStringProc;

        /// GetPinnedIntegerProc
        public IntPtr getPinnedIntegerProc;

        /// GetPinnedFloatProc
        public IntPtr getPinnedFloatProc;

        /// GetPinnedUnitFloatProc
        public IntPtr getPinnedUnitFloatProc;
    }

    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct PIDescriptorParameters
    {

        /// int16->short
        public short descriptorParametersVersion;

        /// int16->short
        public PlayInfo playInfo;

        /// int16->short
        public RecordInfo recordInfo;

        /// PIDescriptorHandle->Handle->LPSTR*
        public System.IntPtr descriptor;

        /// WriteDescriptorProcs*
        public System.IntPtr writeDescriptorProcs;

        /// ReadDescriptorProcs*
        public System.IntPtr readDescriptorProcs;
    }


    internal enum RecordInfo : short
    {

        plugInDialogOptional,

        plugInDialogRequired,

        plugInDialogNone,
    }

    internal enum PlayInfo : short
    {

        plugInDialogDontDisplay,

        plugInDialogDisplay,

        plugInDialogSilent,
    }



}
