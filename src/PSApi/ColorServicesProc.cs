/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2026 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

/* Adapted from PIGeneral.h
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

using System.Runtime.InteropServices;
namespace PSFilterLoad.PSApi
{
    internal enum SpecialColorID : int
    {
        ForegroundColor = 0,
        BackgroundColor = 1
    }

    internal enum ColorServicesSelector : short
    {
        ChooseColor = 0,
        ConvertColor = 1,
        SamplePoint = 2,
        GetSpecialColor = 3
    }

    internal enum ColorSpace : short
    {
        ChosenSpace = -1,
        RGBSpace = 0,
        HSBSpace = 1,
        CMYKSpace = 2,
        LabSpace = 3,
        GraySpace = 4,
        HSLSpace = 5,
        XYZSpace = 6
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct SelectorParameters
    {
        [FieldOffset(0)]
        public nint pickerPrompt;
        [FieldOffset(0)]
        public nint globalSamplePoint;
        [FieldOffset(0)]
        public SpecialColorID specialColorID;
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal unsafe delegate short ColorServicesProc(ColorServicesInfo* info);

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct ColorServicesInfo
    {
        public int infoSize;
        public ColorServicesSelector selector;
        public ColorSpace sourceSpace;
        public ColorSpace resultSpace;
        public PSBoolean resultGamutInfoValid;
        public PSBoolean resultInGamut;
        public nint reservedSourceSpaceInfo;
        public nint reservedResultSpaceInfo;
        public fixed short colorComponents[4];
        public nint reserved;
        public SelectorParameters selectorParameter;
    }
}
