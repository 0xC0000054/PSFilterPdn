/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2023 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterLoad.PSApi.Diagnostics;
using PSFilterLoad.PSApi.Imaging;
using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    internal sealed class ChannelPortsSuite : IDisposable
    {
        private readonly IFilterImageProvider filterImageProvider;
        private readonly ReadPixelsProc readPixelsProc;
        private readonly WriteBasePixelsProc writeBasePixelsProc;
        private readonly ReadPortForWritePortProc readPortForWritePortProc;
        private readonly IPluginApiLogger logger;

        private ISurface<ImageSurface>? scaledChannelSurface;
        private ISurface<MaskSurface>? scaledSelectionMask;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ChannelPortsSuite"/> class.
        /// </summary>
        /// <param name="filterImageProvider">The filter image provider.</param>
        /// <param name="logger">The logger instance.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="filterImageProvider"/> is null.
        /// or
        /// <paramref name="logger"/> is null.
        /// </exception>
        public unsafe ChannelPortsSuite(IFilterImageProvider filterImageProvider, IPluginApiLogger logger)
        {
            ArgumentNullException.ThrowIfNull(filterImageProvider);
            ArgumentNullException.ThrowIfNull(logger);

            this.filterImageProvider = filterImageProvider;
            this.logger = logger;
            readPixelsProc = new ReadPixelsProc(ReadPixelsProc);
            writeBasePixelsProc = new WriteBasePixelsProc(WriteBasePixels);
            readPortForWritePortProc = new ReadPortForWritePortProc(ReadPortForWritePort);
            scaledChannelSurface = null;
            scaledSelectionMask = null;
            disposed = false;
        }

        public unsafe IntPtr CreateChannelPortsPointer()
        {
            IntPtr channelPortsPtr = Memory.Allocate(Marshal.SizeOf<ChannelPortProcs>(), MemoryAllocationOptions.ZeroFill);

            ChannelPortProcs* channelPorts = (ChannelPortProcs*)channelPortsPtr.ToPointer();
            channelPorts->channelPortProcsVersion = PSConstants.kCurrentChannelPortProcsVersion;
            channelPorts->numChannelPortProcs = PSConstants.kCurrentChannelPortProcsCount;
            channelPorts->readPixelsProc = new UnmanagedFunctionPointer<ReadPixelsProc>(readPixelsProc);
            channelPorts->writeBasePixelsProc = new UnmanagedFunctionPointer<WriteBasePixelsProc>(writeBasePixelsProc);
            channelPorts->readPortForWritePortProc = new UnmanagedFunctionPointer<ReadPortForWritePortProc>(readPortForWritePortProc);

            return channelPortsPtr;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;

                if (scaledChannelSurface != null)
                {
                    scaledChannelSurface.Dispose();
                    scaledChannelSurface = null;
                }

                if (scaledSelectionMask != null)
                {
                    scaledSelectionMask.Dispose();
                    scaledSelectionMask = null;
                }
            }
        }

        private static unsafe void FillChannelData(int channel, PixelMemoryDesc* destiniation, ISurface<ImageSurface> source, VRect srcRect)
        {
            byte* dstPtr = (byte*)destiniation->data.ToPointer();
            int stride = destiniation->rowBits / 8;
            int bpp = destiniation->colBits / 8;
            int offset = destiniation->bitOffset / 8;

            using (ISurfaceLock sourceLock = source.Lock(SurfaceLockMode.Read))
            {
                int sourceChannelCount = source.ChannelCount;

                for (int y = srcRect.top; y < srcRect.bottom; y++)
                {
                    byte* src = sourceLock.GetPointPointerUnchecked(srcRect.left, y);
                    byte* dst = dstPtr + (y * stride) + offset;

                    for (int x = srcRect.left; x < srcRect.right; x++)
                    {
                        switch (channel)
                        {
                            case PSConstants.ChannelPorts.Red:
                                *dst = src[2];
                                break;
                            case PSConstants.ChannelPorts.Green:
                                *dst = src[1];
                                break;
                            case PSConstants.ChannelPorts.Blue:
                                *dst = src[0];
                                break;
                            case PSConstants.ChannelPorts.Alpha:
                                *dst = src[3];
                                break;
                        }

                        src += sourceChannelCount;
                        dst += bpp;
                    }
                }
            }
        }

        private static unsafe void FillSelectionMask(PixelMemoryDesc* destiniation, ISurface<MaskSurface> mask, VRect srcRect)
        {
            byte* dstPtr = (byte*)destiniation->data.ToPointer();
            int stride = destiniation->rowBits / 8;
            int bpp = destiniation->colBits / 8;
            int offset = destiniation->bitOffset / 8;

            using (ISurfaceLock maskLock = mask.Lock(SurfaceLockMode.Read))
            {
                for (int y = srcRect.top; y < srcRect.bottom; y++)
                {
                    byte* src = maskLock.GetPointPointerUnchecked(srcRect.left, y);
                    byte* dst = dstPtr + (y * stride) + offset;

                    for (int x = srcRect.left; x < srcRect.right; x++)
                    {
                        *dst = *src;

                        src++;
                        dst += bpp;
                    }
                }
            }
        }

        private unsafe short ReadPixelsProc(IntPtr port, PSScaling* scaling, VRect* writeRect, PixelMemoryDesc* destination, VRect* wroteRect)
        {
            logger.Log(PluginApiLogCategory.ChannelPortsSuite,
                       "port: {0}, rect: {1}",
                       port,
                       new PointerAsStringFormatter<VRect>(writeRect));

            if (scaling == null || writeRect == null || destination == null)
            {
                return PSError.paramErr;
            }

            if (destination->depth != 8)
            {
                return PSError.errUnsupportedDepth;
            }

            if ((destination->bitOffset % 8) != 0)
            {
                return PSError.errUnsupportedBitOffset;
            }

            if ((destination->colBits % 8) != 0)
            {
                return PSError.errUnsupportedColBits;
            }

            if ((destination->rowBits % 8) != 0)
            {
                return PSError.errUnsupportedRowBits;
            }

            int channel = port.ToInt32();

            if (channel < PSConstants.ChannelPorts.Red || channel > PSConstants.ChannelPorts.SelectionMask)
            {
                return PSError.errUnknownPort;
            }

            VRect srcRect = scaling->sourceRect;
            VRect dstRect = scaling->destinationRect;

            int srcWidth = srcRect.right - srcRect.left;
            int srcHeight = srcRect.bottom - srcRect.top;
            int dstWidth = dstRect.right - dstRect.left;
            int dstHeight = dstRect.bottom - dstRect.top;

            if (channel == PSConstants.ChannelPorts.SelectionMask)
            {
                if (srcWidth == dstWidth && srcHeight == dstHeight)
                {
                    FillSelectionMask(destination, filterImageProvider.Mask!, srcRect);
                }
                else
                {
                    if ((scaledSelectionMask == null) || scaledSelectionMask.Width != dstWidth || scaledSelectionMask.Height != dstHeight)
                    {
                        if (scaledSelectionMask != null)
                        {
                            scaledSelectionMask.Dispose();
                            scaledSelectionMask = null;
                        }

                        scaledSelectionMask = filterImageProvider.Mask!.CreateScaledSurface(dstWidth, dstHeight);
                    }

                    FillSelectionMask(destination, scaledSelectionMask, dstRect);
                }
            }
            else
            {
                if (srcWidth == dstWidth && srcHeight == dstHeight)
                {
                    FillChannelData(channel, destination, filterImageProvider.Source, srcRect);
                }
                else
                {
                    if ((scaledChannelSurface == null) || scaledChannelSurface.Width != dstWidth || scaledChannelSurface.Height != dstHeight)
                    {
                        if (scaledChannelSurface != null)
                        {
                            scaledChannelSurface.Dispose();
                            scaledChannelSurface = null;
                        }

                        scaledChannelSurface = filterImageProvider.Source.CreateScaledSurface(dstWidth, dstHeight);
                    }

                    FillChannelData(channel, destination, scaledChannelSurface, dstRect);
                }
            }

            if (wroteRect != null)
            {
                *wroteRect = dstRect;
            }

            return PSError.noErr;
        }

        private unsafe short WriteBasePixels(IntPtr port, VRect* writeRect, PixelMemoryDesc srcDesc)
        {
            logger.Log(PluginApiLogCategory.ChannelPortsSuite,
                       "port: {0}, rect: {1}",
                       port,
                       new PointerAsStringFormatter<VRect>(writeRect));

            return PSError.memFullErr;
        }

        private unsafe short ReadPortForWritePort(IntPtr* readPort, IntPtr writePort)
        {
            logger.Log(PluginApiLogCategory.ChannelPortsSuite,
                       "readPort: {0}, writePort: {1}",
                       new PointerAsStringFormatter<IntPtr>(readPort),
                       writePort);

            return PSError.memFullErr;
        }
    }
}
