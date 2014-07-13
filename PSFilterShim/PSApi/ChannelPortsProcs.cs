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

/* Adapted from PIGeneral.h
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
	[StructLayout(LayoutKind.Sequential)]
	internal struct PixelMemoryDesc
	{
		public IntPtr data;
		public int rowBits;
		public int colBits;
		public int bitOffset;
		public int depth;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct PSScaling
	{
		public VRect sourceRect;
		public VRect destinationRect;
	}

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate short ReadPixelsProc(System.IntPtr port, ref PSScaling scaling, ref VRect writeRect, ref PixelMemoryDesc destination, ref VRect wroteRect);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate short WriteBasePixelsProc(System.IntPtr port, ref VRect writeRect, PixelMemoryDesc source);

	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate short ReadPortForWritePortProc(ref System.IntPtr readPort, System.IntPtr writePort);

	[StructLayout(LayoutKind.Sequential)]
	internal struct ChannelPortProcs
	{
		public short channelPortProcsVersion;
		public short numChannelPortProcs;
		public IntPtr readPixelsProc;
		public IntPtr writeBasePixelsProc;
		public IntPtr readPortForWritePortProc;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct ReadChannelDesc
	{
		public int minVersion;
		public int maxVersion;
		public IntPtr next;
		public IntPtr port;
		public VRect bounds;
		public int depth;
		public VPoint tileSize;
		public VPoint tileOrigin;
		public byte target;
		public byte shown;
		public short channelType;
		public IntPtr contextInfo;
		public IntPtr name;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct WriteChannelDesc
	{
		public int minVersion;		/* The minimum and maximum version which */
		public int maxVersion;		/* can be used to interpret this record. */
	
		public IntPtr next;	/* The next descriptor in the list. */
	
		public IntPtr port;	/* The port to use for reading. */
	
		public VRect bounds;			/* The bounds of the channel data. */
		public int depth;			/* The depth of the data. */
	
		public VPoint tileSize;		/* The size of the tiles. */
		public VPoint tileOrigin;		/* The origin for the tiles. */
	
		public short channelType;		/* The channel type. */

		public short padding;			/* Reserved. Defaults to zero. */
	
		public IntPtr contextInfo;		/* A pointer to additional info dependent on context. */
	
		public IntPtr name;		/* The name of the channel. */
	
	}


	internal static class ChannelTypes
	{
		public const int ctUnspecified = 0;
		public const int ctRed = 1;
		public const int ctGreen = 2;
		public const int ctBlue = 3;
		public const int ctCyan = 4;
		public const int ctMagenta = 5;
		public const int ctYellow = 6;
		public const int ctBlack = 7;
		public const int ctL = 8;
		public const int ctA = 9;
		public const int ctB = 10;
		public const int ctDuotone = 11;
		public const int ctIndex = 12;
		public const int ctBitmap = 13;
		public const int ctColorSelected = 14;
		public const int ctColorProtected = 15;
		public const int ctTransparency = 16;
		public const int ctLayerMask = 17;
		public const int ctInvertedLayerMask = 18;
		public const int ctSelectionMask = 19;
	}

}
