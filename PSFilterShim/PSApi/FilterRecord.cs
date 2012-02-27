/* Adapted from PIFilter.h
 * Copyright (c) 1990-1, Thomas Knoll.
 * Copyright (c) 1992-6, Adobe Systems Incorporated.
 * All rights reserved.
*/


using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate byte TestAbortProc();

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1049:TypesThatOwnNativeResourcesShouldBeDisposable"), StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal unsafe struct FilterRecord
    {
        public int serial;

        public IntPtr abortProc;

        public IntPtr progressProc;

        /// Handle->LPSTR*
        public System.IntPtr parameters;

        /// Point
        public Point16 imageSize;

        /// int16->short
        public short planes;

        /// Rect
        public Rect16 filterRect;

        /// RGBColor
        public RGBColor background;

        /// RGBColor
        public RGBColor foreground;

        /// int32->int
        public int maxSpace;

        /// int32->int
        public int bufferSpace;

        /// Rect
        public Rect16 inRect;

        /// int16->short
        public short inLoPlane;

        /// int16->short
        public short inHiPlane;

        /// Rect
        public Rect16 outRect;

        /// int16->short
        public short outLoPlane;

        /// int16->short
        public short outHiPlane;

        /// void*
        public System.IntPtr inData;

        /// int32->int
        public int inRowBytes;

        /// void*
        public System.IntPtr outData;
        /// int32->int
        public int outRowBytes;
        /// Boolean->BYTE->unsigned char
        public byte isFloating;
        /// Boolean->BYTE->unsigned char
        public byte haveMask;
        /// Boolean->BYTE->unsigned char
        public byte autoMask;
        /// Rect
        public Rect16 maskRect;
        /// void*
        public System.IntPtr maskData;

        /// int32->int
        public int maskRowBytes;

        /// FilterColor->unsigned char[4]
        public fixed byte backColor[4];
       
        /// FilterColor->unsigned char[4] 
        public fixed byte foreColor[4];

        /// OSType->DWORD->unsigned int
        public uint hostSig;
        /// HostProcs
        public IntPtr hostProcs;

        /// int16->short
        public short imageMode;

        /// Fixed->int
        public int imageHRes;

        /// Fixed->int
        public int imageVRes;

        /// Point
        public Point16 floatCoord;

        /// Point
        public Point16 wholeSize;

        /// PlugInMonitor
        public PlugInMonitor monitor;

        /// void*
        public System.IntPtr platformData;

        /// BufferProcs*
        public System.IntPtr bufferProcs;

        /// ResourceProcs*
        public System.IntPtr resourceProcs;
        /// ProcessEventProc
        public IntPtr processEvent;
        /// DisplayPixelsProc
        public IntPtr displayPixels;
        /// HandleProcs*
        public IntPtr handleProcs; 

        /// Boolean->BYTE->unsigned char
        public byte supportsDummyChannels;

        /// Boolean->BYTE->unsigned char
        public byte supportsAlternateLayouts;

        /// int16->short
        public short wantLayout;

        /// int16->short
        public short filterCase;

        /// int16->short
        public short dummyPlaneValue;

        /// void*
        public System.IntPtr premiereHook;

        /// AdvanceStateProc
        public IntPtr advanceState;

        /// Boolean->BYTE->unsigned char
        public byte supportsAbsolute;

        /// Boolean->BYTE->unsigned char
        public byte wantsAbsolute;

        /// GetPropertyProc
        public IntPtr getPropertyObsolete;

        /// Boolean->BYTE->unsigned char
        public byte cannotUndo;

        /// Boolean->BYTE->unsigned char
        public byte supportsPadding;

        /// int16->short
        public short inputPadding;

        /// int16->short
        public short outputPadding;

        /// int16->short
        public short maskPadding;

        /// char
        public byte samplingSupport;

        /// char
        public byte reservedByte;

        /// Fixed->int
        public int inputRate;

        /// Fixed->int
        public int maskRate;

        /// ColorServicesProc
        public IntPtr colorServices;

        /// int16->short
        public short inLayerPlanes;

        /// int16->short
        public short inTransparencyMask;

        /// int16->short
        public short inLayerMasks;

        /// int16->short
        public short inInvertedLayerMasks;

        /// int16->short
        public short inNonLayerPlanes;

        /// int16->short
        public short outLayerPlanes;

        /// int16->short
        public short outTransparencyMask;

        /// int16->short
        public short outLayerMasks;

        /// int16->short
        public short outInvertedLayerMasks;

        /// int16->short
        public short outNonLayerPlanes;

        /// int16->short
        public short absLayerPlanes;

        /// int16->short
        public short absTransparencyMask;

        /// int16->short
        public short absLayerMasks;

        /// int16->short
        public short absInvertedLayerMasks;

        /// int16->short
        public short absNonLayerPlanes;

        /// int16->short
        public short inPreDummyPlanes;

        /// int16->short
        public short inPostDummyPlanes;

        /// int16->short
        public short outPreDummyPlanes;

        /// int16->short
        public short outPostDummyPlanes;

        /// int32->int
        public int inColumnBytes;

        /// int32->int
        public int inPlaneBytes;

        /// int32->int
        public int outColumnBytes;

        /// int32->int
        public int outPlaneBytes;
        /* New in 3.0.4. */
    
        /// ImageServicesProcs*
        public System.IntPtr imageServicesProcs;

        /// PropertyProcs*
        public System.IntPtr propertyProcs;

        /// int16->short
        public short inTileHeight;

        /// int16->short
        public short inTileWidth;

        /// Point
        public Point16 inTileOrigin;

        /// int16->short
        public short absTileHeight;

        /// int16->short
        public short absTileWidth;

        /// Point
        public Point16 absTileOrigin;

        /// int16->short
        public short outTileHeight;

        /// int16->short
        public short outTileWidth;

        /// Point
        public Point16 outTileOrigin;

        /// int16->short
        public short maskTileHeight;

        /// int16->short
        public short maskTileWidth;

        /// Point
        public Point16 maskTileOrigin;

        /*New in 4.0*/

        /// PIDescriptorParameters*
        public System.IntPtr descriptorParameters;

        /// Str255*
        public System.IntPtr errorString;

        /// ChannelPortProcs*
        public System.IntPtr channelPortProcs;

        /// ReadImageDocumentDesc*
        public System.IntPtr documentInfo;

        /* New in  5.0 */

        public IntPtr sSpBasic;

        public IntPtr plugInRef;

        public int depth;

        public fixed byte reserved[66];
    }
}
