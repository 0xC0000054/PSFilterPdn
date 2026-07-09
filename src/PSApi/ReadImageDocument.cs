/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2026 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    internal sealed class ReadImageDocument : Disposable
    {
        private sealed unsafe class ChannelDescPtrs : Disposable
        {
            private ReadChannelDesc* readChannelDesc;
            private IntPtr channelName;

            public ChannelDescPtrs(ReadChannelDesc* readChannelDesc, IntPtr channelName)
            {
                this.readChannelDesc = readChannelDesc;
                this.channelName = channelName;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                {
                }

                Memory.Free(ref readChannelDesc);

                if (channelName != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(channelName);
                    channelName = IntPtr.Zero;
                }
            }
        }

        private readonly int documentWidth;
        private readonly int documentHeight;
        private readonly double dpiX;
        private readonly double dpiY;
        private readonly List<ChannelDescPtrs> channelReadDescPtrs;

        public ReadImageDocument(int documentWidth, int documentHeight, double dpiX, double dpiY)
        {
            this.documentWidth = documentWidth;
            this.documentHeight = documentHeight;
            this.dpiX = dpiX;
            this.dpiY = dpiY;
            channelReadDescPtrs = [];
        }

        public unsafe ReadImageDocumentDesc* CreateReadImageDocumentPointer(FilterCase filterCase, bool hasSelection)
        {
            ReadImageDocumentDesc* readDocumentPtr = Memory.Allocate<ReadImageDocumentDesc>(MemoryAllocationOptions.ZeroFill);

            try
            {
                readDocumentPtr->minVersion = PSConstants.kCurrentMinVersReadImageDocDesc;
                readDocumentPtr->maxVersion = PSConstants.kCurrentMaxVersReadImageDocDesc;
                readDocumentPtr->imageMode = PSConstants.plugInModeRGBColor;
                readDocumentPtr->depth = 8;

                readDocumentPtr->bounds.top = 0;
                readDocumentPtr->bounds.left = 0;
                readDocumentPtr->bounds.right = documentWidth;
                readDocumentPtr->bounds.bottom = documentHeight;
                readDocumentPtr->hResolution = new Fixed16(dpiX);
                readDocumentPtr->vResolution = new Fixed16(dpiY);

                ReadChannelDesc* rgbChannels = CreateReadChannelDesc(PSConstants.ChannelPorts.Red,
                                                                     StringResources.RedChannelName,
                                                                     readDocumentPtr->depth,
                                                                     readDocumentPtr->bounds);

                ReadChannelDesc* ch = rgbChannels;

                for (int i = PSConstants.ChannelPorts.Green; i <= PSConstants.ChannelPorts.Blue; i++)
                {
                    string name;
                    switch (i)
                    {
                        case PSConstants.ChannelPorts.Green:
                            name = StringResources.GreenChannelName;
                            break;
                        case PSConstants.ChannelPorts.Blue:
                            name = StringResources.BlueChannelName;
                            break;
                        default:
                            throw new InvalidOperationException("Unsupported channel index.");
                    }

                    ReadChannelDesc* ptr = CreateReadChannelDesc(i, name, readDocumentPtr->depth, readDocumentPtr->bounds);

                    ch->next = ptr;

                    ch = ptr;
                }

                readDocumentPtr->targetCompositeChannels = readDocumentPtr->mergedCompositeChannels = rgbChannels;

                if (filterCase == FilterCase.EditableTransparencyNoSelection || filterCase == FilterCase.EditableTransparencyWithSelection)
                {
                    ReadChannelDesc* alphaPtr = CreateReadChannelDesc(PSConstants.ChannelPorts.Alpha,
                                                                      StringResources.AlphaChannelName,
                                                                      readDocumentPtr->depth,
                                                                      readDocumentPtr->bounds);
                    readDocumentPtr->targetTransparency = readDocumentPtr->mergedTransparency = alphaPtr;
                }

                if (hasSelection)
                {
                    ReadChannelDesc* selectionPtr = CreateReadChannelDesc(PSConstants.ChannelPorts.SelectionMask,
                                                                          StringResources.SelectionMaskChannelName,
                                                                          readDocumentPtr->depth,
                                                                          readDocumentPtr->bounds);
                    readDocumentPtr->selection = selectionPtr;
                }
            }
            catch (Exception)
            {
                Memory.Free(ref readDocumentPtr);
                throw;
            }

            return readDocumentPtr;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                for (int i = 0; i < channelReadDescPtrs.Count; i++)
                {
                    channelReadDescPtrs[i].Dispose();
                }
            }
        }

        private unsafe ReadChannelDesc* CreateReadChannelDesc(int channel, string name, int depth, VRect bounds)
        {
            ReadChannelDesc* descPtr = Memory.Allocate<ReadChannelDesc>(MemoryAllocationOptions.ZeroFill);

            IntPtr namePtr = IntPtr.Zero;
            try
            {
                namePtr = Marshal.StringToHGlobalAnsi(name);

                channelReadDescPtrs.Add(new ChannelDescPtrs(descPtr, namePtr));
            }
            catch (Exception)
            {
                Memory.Free(ref descPtr);
                if (namePtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(namePtr);
                }
                throw;
            }

            descPtr->minVersion = PSConstants.kCurrentMinVersReadChannelDesc;
            descPtr->maxVersion = PSConstants.kCurrentMaxVersReadChannelDesc;
            descPtr->depth = depth;
            descPtr->bounds = bounds;

            descPtr->target = channel < PSConstants.ChannelPorts.Alpha;
            descPtr->shown = channel < PSConstants.ChannelPorts.SelectionMask;

            descPtr->tileOrigin.h = 0;
            descPtr->tileOrigin.v = 0;
            descPtr->tileSize.h = Math.Min(bounds.right - bounds.left, 1024);
            descPtr->tileSize.v = Math.Min(bounds.bottom - bounds.top, 1024);

            descPtr->port = new IntPtr(channel);
            switch (channel)
            {
                case PSConstants.ChannelPorts.Red:
                    descPtr->channelType = ChannelTypes.Red;
                    break;
                case PSConstants.ChannelPorts.Green:
                    descPtr->channelType = ChannelTypes.Green;
                    break;
                case PSConstants.ChannelPorts.Blue:
                    descPtr->channelType = ChannelTypes.Blue;
                    break;
                case PSConstants.ChannelPorts.Alpha:
                    descPtr->channelType = ChannelTypes.Transparency;
                    break;
                case PSConstants.ChannelPorts.SelectionMask:
                    descPtr->channelType = ChannelTypes.SelectionMask;
                    break;
            }
            descPtr->name = namePtr;

            return descPtr;
        }
    }
}
