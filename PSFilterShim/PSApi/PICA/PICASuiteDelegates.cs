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

/* Adapted from PIBufferSuite.h, PIColorSpaceSuite.h, PIHandleSuite.h, PIUIHooskSuite.h, SPPlugs.h
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
    internal delegate short CSMake(IntPtr colorID);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short CSDelete(IntPtr colorID);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short CSStuffComponents(IntPtr colorID, short colorSpace, ref byte c0, ref byte c1, ref byte c2, ref byte c3);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short CSExtractComponents(IntPtr colorID, short colorSpace, ref byte c0, ref byte c1, ref byte c2, ref byte c3, ref byte gamutFlag);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short CSStuffXYZ(IntPtr colorID, CS_XYZ xyz);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short CSExtractXYZ(IntPtr colorID, ref CS_XYZ xyz);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short CSConvert8(ColorSpace inputCSpace, ColorSpace outputCSpace, IntPtr colorArray, short count);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short CSConvert16(short inputCSpace, short outputCSpace, IntPtr colorArray, short count);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short CSGetNativeSpace(IntPtr colorID, ref short nativeSpace);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short CSIsBookColor(IntPtr colorID, ref byte isBookColor);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short CSExtractColorName(IntPtr colorID, ref IntPtr colorName);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short CSPickColor(IntPtr colorID, IntPtr promptString);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short CSConvert(IntPtr inputData, IntPtr outputData, short count);
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
    internal delegate short UISuiteHostSetCursor(IntPtr cursor);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate uint UISuiteHostTickCount();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate short UISuiteGetPluginName(IntPtr plugInRef, ref IntPtr plugInName);
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
}
