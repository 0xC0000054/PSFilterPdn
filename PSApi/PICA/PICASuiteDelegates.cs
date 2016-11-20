/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2016 Nicholas Hayes
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

#if PICASUITEDEBUG
    #region ColorSpaceSuite Delegates
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSMake(ref IntPtr colorID);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSDelete(ref IntPtr colorID);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSStuffComponents(IntPtr colorID, ColorSpace colorSpace, byte c0, byte c1, byte c2, byte c3);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSExtractComponents(IntPtr colorID, ColorSpace colorSpace, ref byte c0, ref byte c1, ref byte c2, ref byte c3, ref byte gamutFlag);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSStuffXYZ(IntPtr colorID, CS_XYZ xyz);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSExtractXYZ(IntPtr colorID, ref CS_XYZ xyz);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSConvert8(ColorSpace inputCSpace, ColorSpace outputCSpace, IntPtr colorArray, short count);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSConvert16(ColorSpace inputCSpace, ColorSpace outputCSpace, IntPtr colorArray, short count);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSGetNativeSpace(IntPtr colorID, ref ColorSpace nativeSpace);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSIsBookColor(IntPtr colorID, [MarshalAs(UnmanagedType.U1)] ref bool isBookColor);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSExtractColorName(IntPtr colorID, ref IntPtr colorName);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSPickColor(ref IntPtr colorID, IntPtr promptString);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSConvert(IntPtr inputData, IntPtr outputData, short count);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int CSConvertToMonitorRGB(ColorSpace inputCSpace, IntPtr inputData, IntPtr outputData, short count);
    #endregion
#endif

    #region HandleSuite Delegates
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate void SetPIHandleLockDelegate(IntPtr handle, byte lockHandle, ref IntPtr address, ref byte oldLock);
    #endregion

    #region UIHooks Delegates
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr UISuiteMainWindowHandle();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int UISuiteHostSetCursor(IntPtr cursor);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate uint UISuiteHostTickCount();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int UISuiteGetPluginName(IntPtr plugInRef, ref IntPtr plugInName);
    #endregion

#if PICASUITEDEBUG
    #region SPPlugin Delegates
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SPAllocatePluginList(IntPtr strings, ref IntPtr pluginList);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SPFreePluginList(ref IntPtr pluginList);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SPGetPluginListNeededSuiteAvailable(IntPtr pluginList, ref int available);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SPAddPlugin(IntPtr pluginList, IntPtr fileSpec, IntPtr PiPL, IntPtr adapterName, IntPtr adapterInfo, IntPtr plugin);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SPNewPluginListIterator(IntPtr pluginList, ref IntPtr iter);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SPNextPlugin(IntPtr iter, ref IntPtr plugin);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SPDeletePluginListIterator(IntPtr iter);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SPGetHostPluginEntry(IntPtr plugin, ref IntPtr host);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SPGetPluginFileSpecification(IntPtr plugin, ref IntPtr fileSpec);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SPGetPluginPropertyList(IntPtr plugin, ref IntPtr propertyList);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SPGetPluginGlobals(IntPtr plugin, ref IntPtr globals);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SPSetPluginGlobals(IntPtr plugin, IntPtr globals);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SPGetPluginStarted(IntPtr plugin, ref int started);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SPSetPluginStarted(IntPtr plugin, long started);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SPGetPluginSkipShutdown(IntPtr plugin, ref int skipShutdown);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SPSetPluginSkipShutdown(IntPtr plugin, long skipShutdown);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SPGetPluginBroken(IntPtr plugin, ref int broken);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SPSetPluginBroken(IntPtr plugin, long broken);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SPGetPluginAdapter(IntPtr plugin, ref IntPtr adapter);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SPGetPluginAdapterInfo(IntPtr plugin, ref IntPtr adapterInfo);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SPSetPluginAdapterInfo(IntPtr plugin, IntPtr adapterInfo);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SPFindPluginProperty(IntPtr plugin, uint vendorID, uint propertyKey, long propertyID, ref IntPtr p);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SPGetPluginName(IntPtr plugin, ref IntPtr name);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SPSetPluginName(IntPtr plugin, IntPtr name);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SPGetNamedPlugin(IntPtr name, ref IntPtr plugin);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int SPSetPluginPropertyList(IntPtr plugin, IntPtr file);
    #endregion
#endif

    #region ASZStringSuite Delegates
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringMakeFromUnicode(IntPtr src, UIntPtr byteCount, ref IntPtr newZString);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringMakeFromCString(IntPtr src, UIntPtr byteCount, ref IntPtr newZString);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringMakeFromPascalString(IntPtr src, UIntPtr byteCount, ref IntPtr newZString);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringMakeRomanizationOfInteger(int value, ref IntPtr newZString);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringMakeRomanizationOfFixed(int value, short places, [MarshalAs(UnmanagedType.Bool)] bool trim,
                                                           [MarshalAs(UnmanagedType.Bool)] bool isSigned, ref IntPtr newZString);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringMakeRomanizationOfDouble(double value, ref IntPtr newZString);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr ASZStringGetEmpty();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringCopy(IntPtr source, ref IntPtr copy);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringReplace(IntPtr zstr, uint index, IntPtr replacement);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringTrimEllipsis(IntPtr zstr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringTrimSpaces(IntPtr zstr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringRemoveAccelerators(IntPtr zstr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringAddRef(IntPtr zstr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringRelease(IntPtr zstr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal delegate bool ASZStringIsAllWhiteSpace(IntPtr zstr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal delegate bool ASZStringIsEmpty(IntPtr zstr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal delegate bool ASZStringWillReplace(IntPtr zstr, uint index);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate uint ASZStringLengthAsUnicodeCString(IntPtr zstr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringAsUnicodeCString(IntPtr zstr, IntPtr str, uint strSize, [MarshalAs(UnmanagedType.Bool)] bool checkStrSize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate uint ASZStringLengthAsCString(IntPtr zstr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringAsCString(IntPtr zstr, IntPtr str, uint strSize, [MarshalAs(UnmanagedType.Bool)] bool checkStrSize);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate uint ASZStringLengthAsPascalString(IntPtr zstr);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate int ASZStringAsPascalString(IntPtr zstr, IntPtr str, uint strBufferSize, [MarshalAs(UnmanagedType.Bool)] bool checkBufferSize);
    #endregion
}
