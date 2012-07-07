/* Adapted from PIGeneral.h
 * Copyright (c) 1992-1996, Adobe Systems Incorporated.
 * All rights reserved.
*/

using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
	[StructLayout(LayoutKind.Sequential)]
	internal struct PixelMemoryDesc
	{
		/// void*
		public IntPtr data;

		/// int32->int
		public int rowBits;

		/// int32->int
		public int colBits;

		/// int32->int
		public int bitOffset;

		/// int32->int
		public int depth;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct PSScaling
	{

		/// VRect->Anonymous_4d95a2fc_62a6_482f_8501_a4967ca596d5
		public VRect sourceRect;

		/// VRect->Anonymous_4d95a2fc_62a6_482f_8501_a4967ca596d5
		public VRect destinationRect;
	}

	/// Return Type: OSErr->short
	///port: ChannelReadPort->PIChannelPort->_PIChannelPortDesc*
	///scaling: PSScaling*
	///writeRect: VRect*
	///destination: PixelMemoryDesc*
	///wroteRect: VRect*
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate short ReadPixelsProc(System.IntPtr port, ref PSScaling scaling, ref VRect writeRect, ref PixelMemoryDesc destination, ref VRect wroteRect);

	/// Return Type: OSErr->short
	///port: ChannelWritePort->PIChannelPort->_PIChannelPortDesc*
	///writeRect: VRect*
	///source: PixelMemoryDesc*
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate short WriteBasePixelsProc(System.IntPtr port, ref VRect writeRect, PixelMemoryDesc source);

	/// Return Type: OSErr->short
	///readPort: ChannelReadPort*
	///writePort: ChannelWritePort->PIChannelPort->_PIChannelPortDesc*
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	internal delegate short ReadPortForWritePortProc(ref System.IntPtr readPort, System.IntPtr writePort);

	[StructLayout(LayoutKind.Sequential)]
	internal struct ChannelPortProcs
	{

		/// int16->short
		public short channelPortProcsVersion;

		/// int16->short
		public short numChannelPortProcs;

		/// ReadPixelsProc
		public IntPtr readPixelsProc;

		/// WriteBasePixelsProc
		public IntPtr writeBasePixelsProc;

		/// ReadPortForWritePortProc
		public IntPtr readPortForWritePortProc;
	}

	[StructLayout(LayoutKind.Sequential)]
	internal struct ReadChannelDesc
	{
		/// int32->int
		public int minVersion;

		/// int32->int
		public int maxVersion;

		/// ReadChannelDesc*
		public IntPtr next;

		/// PIChannelPort->_PIChannelPortDesc*
		public IntPtr port;

		/// VRect->Anonymous_4d95a2fc_62a6_482f_8501_a4967ca596d5
		public VRect bounds;

		/// int32->int
		public int depth;

		/// VPoint
		public VPoint tileSize;

		/// VPoint
		public VPoint tileOrigin;

		/// Boolean->BYTE->unsigned char
		public byte target;

		/// Boolean->BYTE->unsigned char
		public byte shown;

		/// int16->short
		public short channelType;

		/// void*
		public IntPtr contextInfo;

		/// char*
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
		/// ctUnspecified -> 0
		public const int ctUnspecified = 0;

		/// ctRed -> 1
		public const int ctRed = 1;

		/// ctGreen -> 2
		public const int ctGreen = 2;

		/// ctBlue -> 3
		public const int ctBlue = 3;

		/// ctCyan -> 4
		public const int ctCyan = 4;

		/// ctMagenta -> 5
		public const int ctMagenta = 5;

		/// ctYellow -> 6
		public const int ctYellow = 6;

		/// ctBlack -> 7
		public const int ctBlack = 7;

		/// ctL -> 8
		public const int ctL = 8;

		/// ctA -> 9
		public const int ctA = 9;

		/// ctB -> 10
		public const int ctB = 10;

		/// ctDuotone -> 11
		public const int ctDuotone = 11;

		/// ctIndex -> 12
		public const int ctIndex = 12;

		/// ctBitmap -> 13
		public const int ctBitmap = 13;

		/// ctColorSelected -> 14
		public const int ctColorSelected = 14;

		/// ctColorProtected -> 15
		public const int ctColorProtected = 15;

		/// ctTransparency -> 16
		public const int ctTransparency = 16;

		/// ctLayerMask -> 17
		public const int ctLayerMask = 17;

		/// ctInvertedLayerMask -> 18
		public const int ctInvertedLayerMask = 18;

		/// ctSelectionMask -> 19
		public const int ctSelectionMask = 19;
	}

}
