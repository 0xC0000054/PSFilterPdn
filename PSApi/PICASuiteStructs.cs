/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2015 Nicholas Hayes
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

namespace PSFilterLoad.PSApi
{

	#region BufferSuite1 Delegates
	[UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
	internal delegate IntPtr PSBufferSuiteNew(ref uint requestedSize, uint minimumSize);
	[UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
	internal delegate void PSBufferSuiteDispose(ref IntPtr buffer);
	[UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
	internal delegate uint PSBufferSuiteGetSize(IntPtr buffer);
	[UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
	internal delegate uint PSBufferSuiteGetSpace();
	#endregion
	internal struct PSBufferSuite1
	{
		public IntPtr New;
		public IntPtr Dispose;
		public IntPtr GetSize;
		public IntPtr GetSpace;
	}


#if PICASUITEDEBUG
    internal struct CS_XYZ
    {
        public ushort x; // all clamped to between 0 and 255, why is a ushort used instead of a byte?
        public ushort y;
        public ushort z;
    }

    internal struct CS_Color8
    {
        public byte c0;
        public byte c1;
        public byte c2;
        public byte c3;
    }

    internal struct CS_Color16
    {
        public ushort c0;
        public ushort c1;
        public ushort c2;
        public ushort c3;
    }

    #region ColorSpace1 Delegates
    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short CSMake(IntPtr colorID);
    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short CSDelete(IntPtr colorID);
    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short CSStuffComponents(IntPtr colorID, short colorSpace, ref byte c0, ref byte c1, ref byte c2, ref byte c3);
    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short CSExtractComponents(IntPtr colorID, short colorSpace, ref byte c0, ref byte c1, ref byte c2, ref byte c3, ref byte gamutFlag);
    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short CSStuffXYZ(IntPtr colorID, CS_XYZ xyz);
    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short CSExtractXYZ(IntPtr colorID, ref CS_XYZ xyz);
    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short CSConvert8(ColorSpace inputCSpace, ColorSpace outputCSpace, IntPtr colorArray, short count);
    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short CSConvert16(short inputCSpace, short outputCSpace, IntPtr colorArray, short count);
    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short CSGetNativeSpace(IntPtr colorID, ref short nativeSpace);
    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short CSIsBookColor(IntPtr colorID, ref byte isBookColor);
    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short CSExtractColorName(IntPtr colorID, ref IntPtr colorName);
    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short CSPickColor(IntPtr colorID, IntPtr promptString);
    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate short CSConvert(IntPtr inputData, IntPtr outputData, short count);
    #endregion
    internal struct PSColorSpaceSuite1
    {
        public IntPtr Make;
        public IntPtr Delete;
        public IntPtr StuffComponents;
        public IntPtr ExtractComponents;
        public IntPtr StuffXYZ;
        public IntPtr ExtractXYZ;
        public IntPtr Convert8;
        public IntPtr Convert16;
        public IntPtr GetNativeSpace;
        public IntPtr IsBookColor;
        public IntPtr ExtractColorName;
        public IntPtr PickColor;
        public IntPtr Convert8to16;
        public IntPtr Convert16to8;
    }  
#endif

	[UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
	internal delegate void SetPIHandleLockDelegate(IntPtr handle, byte lockHandle, ref IntPtr address, ref byte oldLock);
	internal struct PSHandleSuite1
	{
		public IntPtr New;
		public IntPtr Dispose;
		public IntPtr SetLock;
		public IntPtr GetSize;
		public IntPtr SetSize;
		public IntPtr RecoverSpace;
	}
	internal struct PSHandleSuite2
	{
		public IntPtr New;
		public IntPtr Dispose;
		public IntPtr DisposeRegularHandle;
		public IntPtr SetLock;
		public IntPtr GetSize;
		public IntPtr SetSize;
		public IntPtr RecoverSpace;
	}

	#region UIHooks Delegates
	[UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
	internal delegate IntPtr UISuiteMainWindowHandle();
	[UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
	internal delegate short UISuiteHostSetCursor(IntPtr cursor);
	[UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
	internal delegate uint UISuiteHostTickCount();
	[UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl), System.Security.SuppressUnmanagedCodeSecurity]
	internal delegate short UISuiteGetPluginName(IntPtr plugInRef, ref IntPtr plugInName);
	#endregion
	internal struct PSUIHooksSuite1
	{
		public IntPtr processEvent;
		public IntPtr displayPixels;
		public IntPtr progressBar;
		public IntPtr testAbort;
		public IntPtr MainAppWindow;
		public IntPtr SetCursor;
		public IntPtr TickCount;
		public IntPtr GetPluginName;
	}

#if PICASUITEDEBUG
    #region SPPlugin Delegates
    internal delegate int SPAllocatePluginList(IntPtr strings, ref IntPtr pluginList);
    internal delegate int SPFreePluginList(ref IntPtr pluginList);
    internal delegate int SPGetPluginListNeededSuiteAvailable(IntPtr pluginList, ref int available);

    internal delegate int SPAddPlugin(IntPtr pluginList, IntPtr fileSpec, IntPtr PiPL, IntPtr adapterName, IntPtr adapterInfo, IntPtr plugin);

    internal delegate int SPNewPluginListIterator(IntPtr pluginList, ref IntPtr iter);
    internal delegate int SPNextPlugin(IntPtr iter, ref IntPtr plugin);
    internal delegate int SPDeletePluginListIterator(IntPtr iter);

    internal delegate int SPGetHostPluginEntry(IntPtr plugin, ref IntPtr host);
    internal delegate int SPGetPluginFileSpecification(IntPtr plugin, ref IntPtr fileSpec);
    internal delegate int SPGetPluginPropertyList(IntPtr plugin, ref IntPtr propertyList);
    internal delegate int SPGetPluginGlobals(IntPtr plugin, ref IntPtr globals);
    internal delegate int SPSetPluginGlobals(IntPtr plugin, IntPtr globals);
    internal delegate int SPGetPluginStarted(IntPtr plugin, ref int started);
    internal delegate int SPSetPluginStarted(IntPtr plugin, long started);
    internal delegate int SPGetPluginSkipShutdown(IntPtr plugin, ref int skipShutdown);
    internal delegate int SPSetPluginSkipShutdown(IntPtr plugin, long skipShutdown);
    internal delegate int SPGetPluginBroken(IntPtr plugin, ref int broken);
    internal delegate int SPSetPluginBroken(IntPtr plugin, long broken);
    internal delegate int SPGetPluginAdapter(IntPtr plugin, ref IntPtr adapter);
    internal delegate int SPGetPluginAdapterInfo(IntPtr plugin, ref IntPtr adapterInfo);
    internal delegate int SPSetPluginAdapterInfo(IntPtr plugin, IntPtr adapterInfo);

    internal delegate int SPFindPluginProperty(IntPtr plugin, uint vendorID, uint propertyKey, long propertyID, ref IntPtr p);

    internal delegate int SPGetPluginName(IntPtr plugin, ref IntPtr name);
    internal delegate int SPSetPluginName(IntPtr plugin, IntPtr name);
    internal delegate int SPGetNamedPlugin(IntPtr name, ref IntPtr plugin);

    internal delegate int SPSetPluginPropertyList(IntPtr plugin, IntPtr file);
    #endregion

    internal struct SPPluginsSuite4
    {
        public IntPtr AllocatePluginList;
        public IntPtr FreePluginList;

        public IntPtr AddPlugin;

        public IntPtr NewPluginListIterator;
        public IntPtr NextPlugin;
        public IntPtr DeletePluginListIterator;
        public IntPtr GetPluginListNeededSuiteAvailable;

        public IntPtr GetPluginHostEntry;
        public IntPtr GetPluginFileSpecification;
        public IntPtr GetPluginPropertyList;
        public IntPtr GetPluginGlobals;
        public IntPtr SetPluginGlobals;
        public IntPtr GetPluginStarted;
        public IntPtr SetPluginStarted;
        public IntPtr GetPluginSkipShutdown;
        public IntPtr SetPluginSkipShutdown;
        public IntPtr GetPluginBroken;
        public IntPtr SetPluginBroken;
        public IntPtr GetPluginAdapter;
        public IntPtr GetPluginAdapterInfo;
        public IntPtr SetPluginAdapterInfo;

        public IntPtr FindPluginProperty;

        public IntPtr GetPluginName;
        public IntPtr SetPluginName;
        public IntPtr GetNamedPlugin;

        public IntPtr SetPluginPropertyList;
    } 
#endif

   
}
