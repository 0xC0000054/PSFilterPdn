using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{

#if PSSDK4
    /// Return Type: PIWriteDescriptor->PIOpaqueWriteDescriptor*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate System.IntPtr OpenWriteDescriptorProc();

    /// Return Type: OSErr->short
    ///param0: PIWriteDescriptor->PIOpaqueWriteDescriptor*
    ///param1: PIDescriptorHandle*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short CloseWriteDescriptorProc(System.IntPtr param0, ref System.IntPtr param1);

    /// Return Type: OSErr->short
    ///param0: PIWriteDescriptor->PIOpaqueWriteDescriptor*
    ///param1: DescriptorKeyID->unsigned int
    ///param2: int32->int
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutIntegerProc([In()]System.IntPtr param0, [In()]uint param1, [In()]int param2);

    /// Return Type: OSErr->short
    ///param0: PIWriteDescriptor->PIOpaqueWriteDescriptor*
    ///param1: DescriptorKeyID->unsigned int
    ///param2: double*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutFloatProc(System.IntPtr param0, uint param1, ref double param2);

    /// Return Type: OSErr->short
    ///param0: PIWriteDescriptor->PIOpaqueWriteDescriptor*
    ///param1: DescriptorKeyID->unsigned int
    ///param2: DescriptorUnitID->unsigned int
    ///param3: double*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutUnitFloatProc([In()]System.IntPtr param0, uint param1, uint param2, ref double param3);

    /// Return Type: OSErr->short
    ///param0: PIWriteDescriptor->PIOpaqueWriteDescriptor*
    ///param1: DescriptorKeyID->unsigned int
    ///param2: Boolean->BYTE->unsigned char
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutBooleanProc([In()]System.IntPtr param0, uint param1, [In()]byte param2);

    /// Return Type: OSErr->short
    ///param0: PIWriteDescriptor->PIOpaqueWriteDescriptor*
    ///param1: DescriptorKeyID->unsigned int
    ///param2: Handle->LPSTR*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutTextProc([In()]System.IntPtr param0, [In()]uint param1, [In(), MarshalAs(UnmanagedType.SysInt)]IntPtr param2);

    /// Return Type: OSErr->short
    ///param0: PIWriteDescriptor->PIOpaqueWriteDescriptor*
    ///param1: DescriptorKeyID->unsigned int
    ///param2: Handle->LPSTR*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutAliasProc(System.IntPtr param0, uint param1, [In()]System.IntPtr param2);

    /// Return Type: OSErr->short
    ///param0: PIWriteDescriptor->PIOpaqueWriteDescriptor*
    ///key: DescriptorKeyID->unsigned int
    ///type: DescriptorTypeID->unsigned int
    ///value: DescriptorEnumID->unsigned int
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutEnumeratedProc(System.IntPtr param0, uint key, uint type, uint value);

    /// Return Type: OSErr->short
    ///param0: PIWriteDescriptor->PIOpaqueWriteDescriptor*
    ///param1: DescriptorKeyID->unsigned int
    ///param2: DescriptorTypeID->unsigned int
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutClassProc(System.IntPtr param0, uint param1, uint param2);

    /// Return Type: OSErr->short
    ///param0: PIWriteDescriptor->PIOpaqueWriteDescriptor*
    ///param1: DescriptorKeyID->unsigned int
    ///param2: PIDescriptorSimpleReference*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutSimpleReferenceProc(System.IntPtr param0, uint param1, ref PIDescriptorSimpleReference param2);

    /// Return Type: OSErr->short
    ///param0: PIWriteDescriptor->PIOpaqueWriteDescriptor*
    ///param1: DescriptorKeyID->unsigned int
    ///param2: DescriptorTypeID->unsigned int
    ///param3: PIDescriptorHandle->Handle->LPSTR*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutObjectProc(System.IntPtr param0, uint param1, uint param2, System.IntPtr param3);

    /// Return Type: OSErr->short
    ///param0: PIWriteDescriptor->PIOpaqueWriteDescriptor*
    ///param1: DescriptorKeyID->unsigned int
    ///count: uint32->unsigned int
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutCountProc(System.IntPtr param0, uint param1, uint count);

    /// Return Type: OSErr->short
    ///param0: PIWriteDescriptor->PIOpaqueWriteDescriptor*
    ///param1: DescriptorKeyID->unsigned int
    ///param2: ConstStr255Param->char*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutStringProc(System.IntPtr param0, uint param1, IntPtr param2);

    /// Return Type: OSErr->short
    ///param0: PIWriteDescriptor->PIOpaqueWriteDescriptor*
    ///param1: DescriptorKeyID->unsigned int
    ///param2: DescriptorTypeID->unsigned int
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutScopedClassProc(System.IntPtr param0, uint param1, uint param2);

    /// Return Type: OSErr->short
    ///param0: PIWriteDescriptor->PIOpaqueWriteDescriptor*
    ///param1: DescriptorKeyID->unsigned int
    ///param2: DescriptorTypeID->unsigned int
    ///param3: PIDescriptorHandle->Handle->LPSTR*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short PutScopedObjectProc(System.IntPtr param0, uint param1, uint param2, ref System.IntPtr param3);

    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Ansi)]
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

    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
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
    ///param0: PIDescriptorHandle->Handle->LPSTR*
    ///param1: DescriptorKeyIDArray->DescriptorKeyID[]
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate System.IntPtr OpenReadDescriptorProc(ref System.IntPtr param0, IntPtr param1);

    /// Return Type: OSErr->short
    ///param0: PIReadDescriptor->PIOpaqueReadDescriptor*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short CloseReadDescriptorProc(System.IntPtr param0);

    /// Return Type: Boolean->BYTE->unsigned char
    ///param0: PIReadDescriptor->PIOpaqueReadDescriptor*
    ///key: DescriptorKeyID*
    ///type: DescriptorTypeID*
    ///flags: int32*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate byte GetKeyProc(System.IntPtr param0, ref uint key, ref uint type, ref int flags);

    /// Return Type: OSErr->short
    ///param0: PIReadDescriptor->PIOpaqueReadDescriptor*
    ///param1: int32*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetIntegerProc(System.IntPtr param0, ref int param1);

    /// Return Type: OSErr->short
    ///param0: PIReadDescriptor->PIOpaqueReadDescriptor*
    ///param1: double*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetFloatProc(System.IntPtr param0, ref double param1);

    /// Return Type: OSErr->short
    ///param0: PIReadDescriptor->PIOpaqueReadDescriptor*
    ///param1: DescriptorUnitID*
    ///param2: double*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetUnitFloatProc(System.IntPtr param0, ref uint param1, ref double param2);

    /// Return Type: OSErr->short
    ///param0: PIReadDescriptor->PIOpaqueReadDescriptor*
    ///param1: Boolean*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetBooleanProc(System.IntPtr param0, ref byte param1);

    /// Return Type: OSErr->short
    ///param0: PIReadDescriptor->PIOpaqueReadDescriptor*
    ///param1: Handle*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetTextProc(System.IntPtr param0, ref System.IntPtr param1);

    /// Return Type: OSErr->short
    ///param0: PIReadDescriptor->PIOpaqueReadDescriptor*
    ///param1: Handle*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetAliasProc(System.IntPtr param0, ref System.IntPtr param1);

    /// Return Type: OSErr->short
    ///param0: PIReadDescriptor->PIOpaqueReadDescriptor*
    ///param1: DescriptorEnumID*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetEnumeratedProc(System.IntPtr param0, ref uint param1);

    /// Return Type: OSErr->short
    ///param0: PIReadDescriptor->PIOpaqueReadDescriptor*
    ///param1: DescriptorTypeID*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetClassProc(System.IntPtr param0, ref uint param1);

    /// Return Type: OSErr->short
    ///param0: PIReadDescriptor->PIOpaqueReadDescriptor*
    ///param1: PIDescriptorSimpleReference*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetSimpleReferenceProc(System.IntPtr param0, ref PIDescriptorSimpleReference param1);

    /// Return Type: OSErr->short
    ///param0: PIReadDescriptor->PIOpaqueReadDescriptor*
    ///param1: DescriptorTypeID*
    ///param2: PIDescriptorHandle*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetObjectProc(System.IntPtr param0, ref uint param1, ref System.IntPtr param2);

    /// Return Type: OSErr->short
    ///param0: PIReadDescriptor->PIOpaqueReadDescriptor*
    ///param1: uint32*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetCountProc(System.IntPtr param0, ref uint param1);

    /// Return Type: OSErr->short
    ///param0: PIReadDescriptor->PIOpaqueReadDescriptor*
    ///param1: Str255*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetStringProc(System.IntPtr param0, System.IntPtr param1);

    /// Return Type: OSErr->short
    ///param0: PIReadDescriptor->PIOpaqueReadDescriptor*
    ///param1: int32->int
    ///param2: int32->int
    ///param3: int32*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetPinnedIntegerProc(System.IntPtr param0, int param1, int param2, ref int param3);

    /// Return Type: OSErr->short
    ///param0: PIReadDescriptor->PIOpaqueReadDescriptor*
    ///param1: double*
    ///param2: double*
    ///param3: double*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetPinnedFloatProc(System.IntPtr param0, ref double param1, ref double param2, ref double param3);

    /// Return Type: OSErr->short
    ///param0: PIReadDescriptor->PIOpaqueReadDescriptor*
    ///param1: double*
    ///param2: double*
    ///param3: DescriptorUnitID*
    ///param4: double*
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short GetPinnedUnitFloatProc(System.IntPtr param0, ref double param1, ref double param2, ref uint param3, ref double param4);

    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
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

    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
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
        public short playInfo;

        /// int16->short
        public short recordInfo;

        /// PIDescriptorHandle->Handle->LPSTR*
        public System.IntPtr descriptor;

        /// WriteDescriptorProcs*
        public System.IntPtr writeDescriptorProcs;

        /// ReadDescriptorProcs*
        public System.IntPtr readDescriptorProcs;
    }


    public enum RecordInfo : short
    {

        plugInDialogOptional,

        plugInDialogRequired,

        plugInDialogNone,
    }

    public enum PlayInfo : short
    {

        plugInDialogDontDisplay,

        plugInDialogDisplay,

        plugInDialogSilent,
    }

#endif


}
