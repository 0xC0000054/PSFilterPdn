/* Adapted from PIGeneral.h
 * Copyright (c) 1992-6, Adobe Systems Incorporated.
 * All rights reserved.
*/

using System.Runtime.InteropServices;
namespace PSFilterLoad.PSApi
{


    [StructLayoutAttribute(LayoutKind.Explicit)]
    internal struct SelectorParameters
    {

        /// Str255*
        [FieldOffsetAttribute(0)]
        public System.IntPtr pickerPrompt;

        /// Point*
        [FieldOffsetAttribute(0)]
        public System.IntPtr globalSamplePoint;

        /// int32->int
        [FieldOffsetAttribute(0)]
        public int specialColorID;
    }
    /// Return Type: OSErr->short
    ///info: ColorServicesInfo*
    [UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.Cdecl)]
    internal delegate short ColorServicesProc(ref ColorServicesInfo info);

    [StructLayoutAttribute(LayoutKind.Sequential)]
    internal unsafe struct ColorServicesInfo
    {

        /// int32->int
        public int infoSize;

        /// int16->short
        public short selector;

        /// int16->short
        public short sourceSpace;

        /// int16->short
        public short resultSpace;

        /// Boolean->BYTE->unsigned char
        public byte resultGamutInfoValid;

        /// Boolean->BYTE->unsigned char
        public byte resultInGamut;

        /// void*
        public System.IntPtr reservedSourceSpaceInfo;

        /// void*
        public System.IntPtr reservedResultSpaceInfo;

        /// int16[4]
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.I2, SizeConst = 4)]
        public short[] colorComponents;

        /// void*
        public System.IntPtr reserved;

        /// SelectorParameters
        public SelectorParameters selectorParameter;
    }

}
