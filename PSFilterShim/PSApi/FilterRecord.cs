/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2020 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

/* Adapted from PIFilter.h
 * Copyright (c) 1990-1991, Thomas Knoll.
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable"), StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal unsafe struct FilterRecord
    {
        public int serial;
        public IntPtr abortProc;
        public IntPtr progressProc;
        public Handle parameters;
        public Point16 imageSize;
        public short planes;
        public Rect16 filterRect;
        public RGBColor background;
        public RGBColor foreground;
        public int maxSpace;
        public int bufferSpace;

        public Rect16 inRect;
        public short inLoPlane;
        public short inHiPlane;

        public Rect16 outRect;
        public short outLoPlane;
        public short outHiPlane;

        public IntPtr inData;
        public int inRowBytes;

        public IntPtr outData;
        public int outRowBytes;

        public PSBoolean isFloating;
        public PSBoolean haveMask;
        public PSBoolean autoMask;
        public Rect16 maskRect;
        public IntPtr maskData;
        public int maskRowBytes;

        public fixed byte backColor[4];
        public fixed byte foreColor[4];
        public uint hostSig;
        public IntPtr hostProcs;

        public short imageMode;
        public Fixed16 imageHRes;
        public Fixed16 imageVRes;
        public Point16 floatCoord;
        public Point16 wholeSize;
        public PlugInMonitor monitor;

        public IntPtr platformData;
        public IntPtr bufferProcs;
        public IntPtr resourceProcs;
        public IntPtr processEvent;
        public IntPtr displayPixels;
        public IntPtr handleProcs;

        /* New in 3.0 */
        public PSBoolean supportsDummyChannels;
        public PSBoolean supportsAlternateLayouts;
        public short wantLayout;

        public FilterCase filterCase;
        public short dummyPlaneValue;
        public IntPtr premiereHook;
        public IntPtr advanceState;

        public PSBoolean supportsAbsolute;
        public PSBoolean wantsAbsolute;
        public IntPtr getPropertyObsolete;
        public PSBoolean cannotUndo;

        public PSBoolean supportsPadding;
        public short inputPadding;
        public short outputPadding;
        public short maskPadding;

        public byte samplingSupport;
        public byte reservedByte;

        public Fixed16 inputRate;
        public Fixed16 maskRate;

        public IntPtr colorServices;

        public short inLayerPlanes;
        public short inTransparencyMask;
        public short inLayerMasks;
        public short inInvertedLayerMasks;
        public short inNonLayerPlanes;

        public short outLayerPlanes;
        public short outTransparencyMask;
        public short outLayerMasks;
        public short outInvertedLayerMasks;
        public short outNonLayerPlanes;

        public short absLayerPlanes;
        public short absTransparencyMask;
        public short absLayerMasks;
        public short absInvertedLayerMasks;
        public short absNonLayerPlanes;

        public short inPreDummyPlanes;
        public short inPostDummyPlanes;
        public short outPreDummyPlanes;
        public short outPostDummyPlanes;

        public int inColumnBytes;
        public int inPlaneBytes;
        public int outColumnBytes;
        public int outPlaneBytes;

        /* New in 3.0.4. */
        public IntPtr imageServicesProcs;
        public IntPtr propertyProcs;

        public short inTileHeight;
        public short inTileWidth;
        public Point16 inTileOrigin;

        public short absTileHeight;
        public short absTileWidth;
        public Point16 absTileOrigin;

        public short outTileHeight;
        public short outTileWidth;
        public Point16 outTileOrigin;

        public short maskTileHeight;
        public short maskTileWidth;
        public Point16 maskTileOrigin;

        /* New in 4.0*/
        public IntPtr descriptorParameters;
        public IntPtr errorString;
        public IntPtr channelPortProcs;
        public IntPtr documentInfo;

        /* New in 5.0 */
        public IntPtr sSPBasic;
        public IntPtr plugInRef;
        public int depth;

        public fixed byte reserved[66];
    }
}
