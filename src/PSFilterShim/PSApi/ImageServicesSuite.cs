/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2022 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterLoad.PSApi.Diagnostics;
using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    internal sealed class ImageServicesSuite
    {
        private readonly PIResampleProc interpolate1DProc;
        private readonly PIResampleProc interpolate2DProc;
        private readonly IPluginApiLogger logger;

        public unsafe ImageServicesSuite(IPluginApiLogger logger)
        {
            ArgumentNullException.ThrowIfNull(logger);

            interpolate1DProc = new PIResampleProc(Interpolate1DProc);
            interpolate2DProc = new PIResampleProc(Interpolate2DProc);
            this.logger = logger;
        }

        public unsafe IntPtr CreateImageServicesSuitePointer()
        {
            IntPtr imageServicesProcsPtr = Memory.Allocate(Marshal.SizeOf<ImageServicesProcs>(), true);

            ImageServicesProcs* imageServicesProcs = (ImageServicesProcs*)imageServicesProcsPtr.ToPointer();

            imageServicesProcs->imageServicesProcsVersion = PSConstants.kCurrentImageServicesProcsVersion;
            imageServicesProcs->numImageServicesProcs = PSConstants.kCurrentImageServicesProcsCount;
            imageServicesProcs->interpolate1DProc = new UnmanagedFunctionPointer<PIResampleProc>(interpolate1DProc);
            imageServicesProcs->interpolate2DProc = new UnmanagedFunctionPointer<PIResampleProc>(interpolate2DProc);

            return imageServicesProcsPtr;
        }

        private unsafe short Interpolate1DProc(PSImagePlane* source,
                                               PSImagePlane* destination,
                                               Rect16* area,
                                               IntPtr coords,
                                               InterpolationMethod method)
        {
            logger.Log(PluginApiLogCategory.ImageServicesSuite,
                       "source: [{0}], destination: [{1}], area: {2}, coords: 0x{3}, method: {4}",
                       new PointerAsStringFormatter<PSImagePlane>(source),
                       new PointerAsStringFormatter<PSImagePlane>(destination),
                       new PointerAsStringFormatter<Rect16>(area),
                       new IntPtrAsHexStringFormatter(coords),
                       method);

            return PSError.memFullErr;
        }

        private unsafe short Interpolate2DProc(PSImagePlane* source,
                                               PSImagePlane* destination,
                                               Rect16* area,
                                               IntPtr coords,
                                               InterpolationMethod method)
        {
            logger.Log(PluginApiLogCategory.ImageServicesSuite,
                       "source: [{0}], destination: [{1}], area: {2}, coords: 0x{3}, method: {4}",
                       new PointerAsStringFormatter<PSImagePlane>(source),
                       new PointerAsStringFormatter<PSImagePlane>(destination),
                       new PointerAsStringFormatter<Rect16>(area),
                       new IntPtrAsHexStringFormatter(coords),
                       method);

            return PSError.memFullErr;
        }
    }
}
