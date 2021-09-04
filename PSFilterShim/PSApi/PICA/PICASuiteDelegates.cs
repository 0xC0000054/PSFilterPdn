/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2021 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

/* Adapted from ASZStringSuite.h, PIBufferSuite.h, PIColorSpaceSuite.h, PIHandleSuite.h, PIUIHooskSuite.h, SPPlugs.h
*  Copyright 1986 - 2000 Adobe Systems Incorporated
*  All Rights Reserved
*/

using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi.PICA
{
    #region BufferSuite Delegates
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr PSBufferSuiteNew(ref uint requestedSize, uint minimumSize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void PSBufferSuiteDispose(ref IntPtr buffer);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate uint PSBufferSuiteGetSize(IntPtr buffer);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate uint PSBufferSuiteGetSpace();
    #endregion

    #region ColorSpaceSuite Delegates
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSMake(ref ColorID colorID);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSDelete(ref ColorID colorID);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSStuffComponents(ColorID colorID, ColorSpace colorSpace, byte c0, byte c1, byte c2, byte c3);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSExtractComponents(ColorID colorID, ColorSpace colorSpace, ref byte c0, ref byte c1, ref byte c2, ref byte c3, ref byte gamutFlag);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSStuffXYZ(ColorID colorID, CS_XYZ xyz);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSExtractXYZ(ColorID colorID, ref CS_XYZ xyz);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSConvert8(ColorSpace inputCSpace, ColorSpace outputCSpace, IntPtr colorArray, short count);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSConvert16(ColorSpace inputCSpace, ColorSpace outputCSpace, IntPtr colorArray, short count);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSGetNativeSpace(ColorID colorID, ref ColorSpace nativeSpace);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSIsBookColor(ColorID colorID, [MarshalAs(UnmanagedType.U1)] ref bool isBookColor);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSExtractColorName(ColorID colorID, ref ASZString colorName);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSPickColor(ref ColorID colorID, ASZString promptString);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSConvert(IntPtr inputData, IntPtr outputData, short count);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSConvertToMonitorRGB(ColorSpace inputCSpace, IntPtr inputData, IntPtr outputData, short count);
    #endregion

    #region HandleSuite Delegates
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void SetPIHandleLockDelegate(Handle handle, byte lockHandle, ref IntPtr address, ref byte oldLock);
    #endregion

    #region UIHooks Delegates
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr UISuiteMainWindowHandle();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int UISuiteHostSetCursor(IntPtr cursor);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate uint UISuiteHostTickCount();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int UISuiteGetPluginName(IntPtr plugInRef, ref ASZString plugInName);
    #endregion

    #region ASZStringSuite Delegates
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringMakeFromUnicode(IntPtr src, UIntPtr byteCount, ref ASZString newZString);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringMakeFromCString(IntPtr src, UIntPtr byteCount, ref ASZString newZString);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringMakeFromPascalString(IntPtr src, UIntPtr byteCount, ref ASZString newZString);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringMakeRomanizationOfInteger(int value, ref ASZString newZString);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringMakeRomanizationOfFixed(int value, short places, [MarshalAs(UnmanagedType.Bool)] bool trim,
                                                           [MarshalAs(UnmanagedType.Bool)] bool isSigned, ref ASZString newZString);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringMakeRomanizationOfDouble(double value, ref ASZString newZString);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate ASZString ASZStringGetEmpty();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringCopy(ASZString source, ref ASZString copy);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringReplace(ASZString zstr, uint index, ASZString replacement);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringTrimEllipsis(ASZString zstr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringTrimSpaces(ASZString zstr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringRemoveAccelerators(ASZString zstr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringAddRef(ASZString zstr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringRelease(ASZString zstr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal delegate bool ASZStringIsAllWhiteSpace(ASZString zstr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal delegate bool ASZStringIsEmpty(ASZString zstr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal delegate bool ASZStringWillReplace(ASZString zstr, uint index);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate uint ASZStringLengthAsUnicodeCString(ASZString zstr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringAsUnicodeCString(ASZString zstr, IntPtr str, uint strSize, [MarshalAs(UnmanagedType.Bool)] bool checkStrSize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate uint ASZStringLengthAsCString(ASZString zstr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringAsCString(ASZString zstr, IntPtr str, uint strSize, [MarshalAs(UnmanagedType.Bool)] bool checkStrSize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate uint ASZStringLengthAsPascalString(ASZString zstr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringAsPascalString(ASZString zstr, IntPtr str, uint strBufferSize, [MarshalAs(UnmanagedType.Bool)] bool checkBufferSize);
    #endregion
}
