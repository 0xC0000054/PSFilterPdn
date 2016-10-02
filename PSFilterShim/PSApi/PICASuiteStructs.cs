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

namespace PSFilterLoad.PSApi
{
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
