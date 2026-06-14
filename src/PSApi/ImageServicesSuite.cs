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

using PSFilterLoad.PSApi.Diagnostics;
using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    internal sealed unsafe class ImageServicesSuite
    {
        private readonly PIResampleProc interpolate1DProc;
        private readonly PIResampleProc interpolate2DProc;
        private readonly IPluginApiLogger logger;

        public ImageServicesSuite(IPluginApiLogger logger)
        {
            ArgumentNullException.ThrowIfNull(logger);

            interpolate1DProc = new PIResampleProc(Interpolate1DProc);
            interpolate2DProc = new PIResampleProc(Interpolate2DProc);
            this.logger = logger;
        }

        public IntPtr CreateImageServicesSuitePointer()
        {
            IntPtr imageServicesProcsPtr = Memory.Allocate(Marshal.SizeOf<ImageServicesProcs>(), MemoryAllocationOptions.ZeroFill);

            ImageServicesProcs* imageServicesProcs = (ImageServicesProcs*)imageServicesProcsPtr;

            imageServicesProcs->imageServicesProcsVersion = PSConstants.kCurrentImageServicesProcsVersion;
            imageServicesProcs->numImageServicesProcs = PSConstants.kCurrentImageServicesProcsCount;
            imageServicesProcs->interpolate1DProc = new UnmanagedFunctionPointer<PIResampleProc>(interpolate1DProc);
            imageServicesProcs->interpolate2DProc = new UnmanagedFunctionPointer<PIResampleProc>(interpolate2DProc);

            return imageServicesProcsPtr;
        }

        private static byte* GetRowAddressUnchecked(PSImagePlane* plane, int y)
        {
            return (byte*)plane->data + y * (long)plane->rowBytes;
        }

        private static byte* GetPointAddressUnchecked(PSImagePlane* plane, int x, int y)
        {
            return GetRowAddressUnchecked(plane, y) + x * (long)plane->colBytes;
        }

        private static int GetPointSampleCoordinate(Fixed16 fixedValue)
        {
            int result;

            if (fixedValue.IsInteger)
            {
                result = fixedValue.ToInt32();
            }
            else
            {
                // The Photoshop SDK documentation states that in the point sampling mode,
                // any floating point values will be rounded to the nearest integer.
                result = (int)Math.Round(fixedValue.ToDouble());
            }

            return result;
        }

        private short Interpolate1DProc(PSImagePlane* source,
                                        PSImagePlane* destination,
                                        Rect16* area,
                                        Fixed16* coordinates,
                                        InterpolationMethod method)
        {
            logger.Log(PluginApiLogCategory.ImageServicesSuite,
                       "source: [{0}], destination: [{1}], area: {2}, coordinates: 0x{3}, method: {4}",
                       new PointerAsStringFormatter<PSImagePlane>(source),
                       new PointerAsStringFormatter<PSImagePlane>(destination),
                       new PointerAsStringFormatter<Rect16>(area),
                       new IntPtrAsHexStringFormatter(new IntPtr(coordinates)),
                       method);

            if (source == null || destination == null || area == null || coordinates == null)
            {
                return PSError.filterBadParameters;
            }

            if (method == InterpolationMethod.PointSampling)
            {
                int areaWidth = area->right - area->left;
                int areaTop = area->top;
                int areaBottom = area->bottom;
                int areaLeft = area->left;
                int areaRight = area->right;
                int sourceTop = source->bounds.top;
                int sourceLeft = source->bounds.left;
                int sourceRight = source->bounds.right;
                int destTop = destination->bounds.top;

                for (int y = areaTop; y < areaBottom; y++)
                {
                    byte* dest = GetRowAddressUnchecked(destination, y - destTop);

                    int sampleRowOffset = (y - areaTop) * areaWidth;

                    for (int x = areaLeft; x < areaRight; x++)
                    {
                        int sampleIndex = (sampleRowOffset + (x - areaLeft));

                        int sampleX = GetPointSampleCoordinate(coordinates[sampleIndex]);

                        if (sampleX >= sourceLeft && sampleX < sourceRight)
                        {
                            byte* src = GetPointAddressUnchecked(source, sampleX - sourceLeft, y - sourceTop);

                            *dest = *src;
                        }

                        dest += destination->colBytes;
                    }
                }
            }
            // TODO: Implement bilinear and bicubic sampling.
            //       The Shear filter (Shear8B.8BF) is currently unusable due to it requiring bicubic sampling.
            //       This would also require updating the value returned for the InterpolationMethod property
            //       in PropertySuite.cs so that filters which use that property would select the higher
            //       quality sampling modes.

            return PSError.memFullErr;
        }

        private short Interpolate2DProc(PSImagePlane* source,
                                        PSImagePlane* destination,
                                        Rect16* area,
                                        Fixed16* coordinates,
                                        InterpolationMethod method)
        {
            logger.Log(PluginApiLogCategory.ImageServicesSuite,
                       "source: [{0}], destination: [{1}], area: {2}, coordinates: 0x{3}, method: {4}",
                       new PointerAsStringFormatter<PSImagePlane>(source),
                       new PointerAsStringFormatter<PSImagePlane>(destination),
                       new PointerAsStringFormatter<Rect16>(area),
                       new IntPtrAsHexStringFormatter(new IntPtr(coordinates)),
                       method);

            if (source == null || destination == null || area == null || coordinates == null)
            {
                return PSError.filterBadParameters;
            }

            if (method == InterpolationMethod.PointSampling)
            {
                int areaWidth = area->right - area->left;
                int areaTop = area->top;
                int areaBottom = area->bottom;
                int areaLeft = area->left;
                int areaRight = area->right;
                int sourceTop = source->bounds.top;
                int sourceLeft = source->bounds.left;
                int sourceBottom = source->bounds.bottom;
                int sourceRight = source->bounds.right;
                int destTop = destination->bounds.top;

                for (int y = areaTop; y < areaBottom; y++)
                {
                    byte* dest = GetRowAddressUnchecked(destination, y - destTop);

                    int sampleRowOffset = (y - areaTop) * areaWidth;

                    for (int x = areaLeft; x < areaRight; x++)
                    {
                        int sampleIndex = (sampleRowOffset + (x - areaLeft)) * 2;

                        int sampleY = GetPointSampleCoordinate(coordinates[sampleIndex]);
                        int sampleX = GetPointSampleCoordinate(coordinates[sampleIndex + 1]);

                        if (sampleY >= sourceTop && sampleY < sourceBottom && sampleX >= sourceLeft && sampleX < sourceRight)
                        {
                            byte* src = GetPointAddressUnchecked(source, sampleX - sourceLeft, sampleY - sourceTop);

                            *dest = *src;
                        }

                        dest += destination->colBytes;
                    }
                }
            }
            // TODO: Implement bilinear and bicubic sampling.
            //       The Shear filter (Shear8B.8BF) is currently unusable due to it requiring bicubic sampling.
            //       This would also require updating the value returned for the InterpolationMethod property
            //       in PropertySuite.cs so that filters which use that property would select the higher
            //       quality sampling modes.

            return PSError.noErr;
        }
    }
}
