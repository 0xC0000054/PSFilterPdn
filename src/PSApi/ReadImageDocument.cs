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

using PSFilterPdn.Properties;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    internal sealed class ReadImageDocument : IDisposable
    {
        private sealed class ChannelDescPtrs : IDisposable
        {
            private IntPtr readChannelDesc;
            private IntPtr channelName;
            private bool disposed;

            public ChannelDescPtrs(IntPtr readChannelDesc, IntPtr channelName)
            {
                this.readChannelDesc = readChannelDesc;
                this.channelName = channelName;
                disposed = false;
            }

            ~ChannelDescPtrs()
            {
                Dispose(false);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                if (!disposed)
                {
                    disposed = true;

                    if (disposing)
                    {
                    }

                    if (readChannelDesc != IntPtr.Zero)
                    {
                        Memory.Free(readChannelDesc);
                        readChannelDesc = IntPtr.Zero;
                    }

                    if (channelName != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(channelName);
                        channelName = IntPtr.Zero;
                    }
                }
            }
        }

        private readonly int documentWidth;
        private readonly int documentHeight;
        private readonly double dpiX;
        private readonly double dpiY;
        private List<ChannelDescPtrs> channelReadDescPtrs;
        private bool disposed;

        public ReadImageDocument(int documentWidth, int documentHeight, double dpiX, double dpiY)
        {
            this.documentWidth = documentWidth;
            this.documentHeight = documentHeight;
            this.dpiX = dpiX;
            this.dpiY = dpiY;
            channelReadDescPtrs = new List<ChannelDescPtrs>();
            disposed = false;
        }

        public unsafe IntPtr CreateReadImageDocumentPointer(FilterCase filterCase, bool hasSelection)
        {
            IntPtr readDocumentPtr = Memory.Allocate(Marshal.SizeOf<ReadImageDocumentDesc>(), true);

            try
            {
                ReadImageDocumentDesc* doc = (ReadImageDocumentDesc*)readDocumentPtr.ToPointer();
                doc->minVersion = PSConstants.kCurrentMinVersReadImageDocDesc;
                doc->maxVersion = PSConstants.kCurrentMaxVersReadImageDocDesc;
                doc->imageMode = PSConstants.plugInModeRGBColor;
                doc->depth = 8;

                doc->bounds.top = 0;
                doc->bounds.left = 0;
                doc->bounds.right = documentWidth;
                doc->bounds.bottom = documentHeight;
                doc->hResolution = new Fixed16((int)(dpiX + 0.5));
                doc->vResolution = new Fixed16((int)(dpiY + 0.5));

                IntPtr channel = CreateReadChannelDesc(PSConstants.ChannelPorts.Red, Resources.RedChannelName, doc->depth, doc->bounds);

                ReadChannelDesc* ch = (ReadChannelDesc*)channel.ToPointer();

                for (int i = PSConstants.ChannelPorts.Green; i <= PSConstants.ChannelPorts.Blue; i++)
                {
                    string name = null;
                    switch (i)
                    {
                        case PSConstants.ChannelPorts.Green:
                            name = Resources.GreenChannelName;
                            break;
                        case PSConstants.ChannelPorts.Blue:
                            name = Resources.BlueChannelName;
                            break;
                    }

                    IntPtr ptr = CreateReadChannelDesc(i, name, doc->depth, doc->bounds);

                    ch->next = ptr;

                    ch = (ReadChannelDesc*)ptr.ToPointer();
                }

                doc->targetCompositeChannels = doc->mergedCompositeChannels = channel;

                if (filterCase == FilterCase.EditableTransparencyNoSelection || filterCase == FilterCase.EditableTransparencyWithSelection)
                {
                    IntPtr alphaPtr = CreateReadChannelDesc(PSConstants.ChannelPorts.Alpha, Resources.AlphaChannelName, doc->depth, doc->bounds);
                    doc->targetTransparency = doc->mergedTransparency = alphaPtr;
                }

                if (hasSelection)
                {
                    IntPtr selectionPtr = CreateReadChannelDesc(PSConstants.ChannelPorts.SelectionMask, Resources.MaskChannelName, doc->depth, doc->bounds);
                    doc->selection = selectionPtr;
                }
            }
            catch (Exception)
            {
                if (readDocumentPtr != IntPtr.Zero)
                {
                    Memory.Free(readDocumentPtr);
                }

                throw;
            }

            return readDocumentPtr;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;

                for (int i = 0; i < channelReadDescPtrs.Count; i++)
                {
                    channelReadDescPtrs[i].Dispose();
                }
            }
        }

        private unsafe IntPtr CreateReadChannelDesc(int channel, string name, int depth, VRect bounds)
        {
            IntPtr addressPtr = Memory.Allocate(Marshal.SizeOf<ReadChannelDesc>(), true);
            IntPtr namePtr = IntPtr.Zero;
            try
            {
                namePtr = Marshal.StringToHGlobalAnsi(name);

                channelReadDescPtrs.Add(new ChannelDescPtrs(addressPtr, namePtr));
            }
            catch (Exception)
            {
                Memory.Free(addressPtr);
                if (namePtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(namePtr);
                }
                throw;
            }

            ReadChannelDesc* desc = (ReadChannelDesc*)addressPtr.ToPointer();
            desc->minVersion = PSConstants.kCurrentMinVersReadChannelDesc;
            desc->maxVersion = PSConstants.kCurrentMaxVersReadChannelDesc;
            desc->depth = depth;
            desc->bounds = bounds;

            desc->target = channel < PSConstants.ChannelPorts.Alpha;
            desc->shown = channel < PSConstants.ChannelPorts.SelectionMask;

            desc->tileOrigin.h = 0;
            desc->tileOrigin.v = 0;
            desc->tileSize.h = Math.Min(bounds.right - bounds.left, 1024);
            desc->tileSize.v = Math.Min(bounds.bottom - bounds.top, 1024);

            desc->port = new IntPtr(channel);
            switch (channel)
            {
                case PSConstants.ChannelPorts.Red:
                    desc->channelType = ChannelTypes.Red;
                    break;
                case PSConstants.ChannelPorts.Green:
                    desc->channelType = ChannelTypes.Green;
                    break;
                case PSConstants.ChannelPorts.Blue:
                    desc->channelType = ChannelTypes.Blue;
                    break;
                case PSConstants.ChannelPorts.Alpha:
                    desc->channelType = ChannelTypes.Transparency;
                    break;
                case PSConstants.ChannelPorts.SelectionMask:
                    desc->channelType = ChannelTypes.SelectionMask;
                    break;
            }
            desc->name = namePtr;

            return addressPtr;
        }
    }
}
