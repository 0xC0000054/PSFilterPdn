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

// Ignore Spelling: Lerp

using PSFilterLoad.PSApi.Diagnostics;
using System;

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

        public ImageServicesProcs* CreateImageServicesSuitePointer()
        {
            ImageServicesProcs* imageServicesProcsPtr = Memory.Allocate<ImageServicesProcs>(MemoryAllocationOptions.ZeroFill);

            imageServicesProcsPtr->imageServicesProcsVersion = PSConstants.kCurrentImageServicesProcsVersion;
            imageServicesProcsPtr->numImageServicesProcs = PSConstants.kCurrentImageServicesProcsCount;
            imageServicesProcsPtr->interpolate1DProc = new UnmanagedFunctionPointer<PIResampleProc>(interpolate1DProc);
            imageServicesProcsPtr->interpolate2DProc = new UnmanagedFunctionPointer<PIResampleProc>(interpolate2DProc);

            return imageServicesProcsPtr;
        }

        private static byte* GetRowAddressUnchecked(PSImagePlane* plane, int y)
        {
            return (byte*)plane->data + y * (long)plane->rowBytes;
        }

        private static byte GetPointClamped(PSImagePlane* plane, int x, int y)
        {
            Rect16 bounds = plane->bounds;

            if (x < bounds.left)
            {
                x = bounds.left;
            }
            else if (x >= bounds.right)
            {
                x = bounds.right - 1;
            }

            if (y < bounds.top)
            {
                y = bounds.top;
            }
            else if (y >= bounds.bottom)
            {
                y = bounds.bottom - 1;
            }

            return GetPointUnchecked(plane, x, y);
        }

        private static byte* GetPointAddressUnchecked(PSImagePlane* plane, int x, int y)
        {
            return GetRowAddressUnchecked(plane, y) + x * (long)plane->colBytes;
        }

        private static byte GetPointUnchecked(PSImagePlane* plane, int x, int y)
        {
            return *GetPointAddressUnchecked(plane, x, y);
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

        private static byte GetResampledValueBilinear(PSImagePlane* plane, int x, int y, double xFrac, double yFrac)
        {
            // Adapted from https://blog.demofox.org/2015/08/15/resizing-images-with-bicubic-interpolation/

            // get pixels
            byte p00 = GetPointClamped(plane, x + 0, y + 0);
            byte p10 = GetPointClamped(plane, x + 1, y + 0);
            byte p01 = GetPointClamped(plane, x + 0, y + 1);
            byte p11 = GetPointClamped(plane, x + 1, y + 1);

            // interpolate bi-linearly!

            double col0 = ResamplingHelpers.Lerp(p00, p10, xFrac);
            double col1 = ResamplingHelpers.Lerp(p01, p11, xFrac);
            double value = ResamplingHelpers.Lerp(col0, col1, yFrac);
  
            return (byte)Math.Clamp(value, 0, 255);
        }

        private static byte GetResampledValueBicubic(PSImagePlane* plane, int x, int y, double xFrac, double yFrac)
        {
            // Adapted from https://blog.demofox.org/2015/08/15/resizing-images-with-bicubic-interpolation/

            // 1st row
            byte p00 = GetPointClamped(plane, x - 1, y - 1);
            byte p10 = GetPointClamped(plane, x + 0, y - 1);
            byte p20 = GetPointClamped(plane, x + 1, y - 1);
            byte p30 = GetPointClamped(plane, x + 2, y - 1);

            // 2nd row
            byte p01 = GetPointClamped(plane, x - 1, y + 0);
            byte p11 = GetPointClamped(plane, x + 0, y + 0);
            byte p21 = GetPointClamped(plane, x + 1, y + 0);
            byte p31 = GetPointClamped(plane, x + 2, y + 0);

            // 3rd row
            byte p02 = GetPointClamped(plane, x - 1, y + 1);
            byte p12 = GetPointClamped(plane, x + 0, y + 1);
            byte p22 = GetPointClamped(plane, x + 1, y + 1);
            byte p32 = GetPointClamped(plane, x + 2, y + 1);

            // 4th row
            byte p03 = GetPointClamped(plane, x - 1, y + 2);
            byte p13 = GetPointClamped(plane, x + 0, y + 2);
            byte p23 = GetPointClamped(plane, x + 1, y + 2);
            byte p33 = GetPointClamped(plane, x + 2, y + 2);

            // interpolate bi-cubically!

            double col0 = ResamplingHelpers.CubicHermite(p00, p10, p20, p30, xFrac);
            double col1 = ResamplingHelpers.CubicHermite(p01, p11, p21, p31, xFrac);
            double col2 = ResamplingHelpers.CubicHermite(p02, p12, p22, p32, xFrac);
            double col3 = ResamplingHelpers.CubicHermite(p03, p13, p23, p33, xFrac);
            double value = ResamplingHelpers.CubicHermite(col0, col1, col2, col3, yFrac);

            return (byte)Math.Clamp(value, 0, 255);
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

            if (source == null
                || destination == null
                || area == null
                || coordinates == null
                || method < InterpolationMethod.PointSampling
                || method > InterpolationMethod.Bicubic)
            {
                return PSError.filterBadParameters;
            }

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

                    Fixed16 coordinateX = coordinates[sampleIndex];

                    if (method == InterpolationMethod.PointSampling)
                    {
                        int sampleX = GetPointSampleCoordinate(coordinateX);

                        if (sampleX >= sourceLeft && sampleX < sourceRight)
                        {
                            *dest = GetPointUnchecked(source, sampleX - sourceLeft, y - sourceTop);
                        }
                    }
                    else // Bilinear or Bicubic
                    {
                        int sampleYInt = y;

                        double sampleX = coordinateX.ToDouble();
                        int sampleXInt = (int)sampleX;

                        if (sampleXInt >= sourceLeft && sampleXInt < sourceRight)
                        {
                            const double sampleYFrac = 0.0;
                            double sampleXFrac = sampleX - Math.Floor(sampleX);

                            switch (method)
                            {
                                case InterpolationMethod.Bilinear:
                                    *dest = GetResampledValueBilinear(source,
                                                                      sampleXInt - sourceLeft,
                                                                      sampleYInt - sourceTop,
                                                                      sampleXFrac,
                                                                      sampleYFrac);
                                    break;
                                case InterpolationMethod.Bicubic:
                                    *dest = GetResampledValueBicubic(source,
                                                                     sampleXInt - sourceLeft,
                                                                     sampleYInt - sourceTop,
                                                                     sampleXFrac,
                                                                     sampleYFrac);
                                    break;
                                case InterpolationMethod.PointSampling:
                                default:
                                    throw new InvalidOperationException($"Unsupported method value: {method}.");
                            }
                        }

                        dest += destination->colBytes;
                    }
                }
            }

            return PSError.noErr;
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

            if (source == null
                || destination == null
                || area == null
                || coordinates == null
                || method < InterpolationMethod.PointSampling
                || method > InterpolationMethod.Bicubic)
            {
                return PSError.filterBadParameters;
            }

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

                    Fixed16 coordinateY = coordinates[sampleIndex];
                    Fixed16 coordinateX = coordinates[sampleIndex + 1];

                    if (method == InterpolationMethod.PointSampling)
                    {
                        int sampleY = GetPointSampleCoordinate(coordinateY);
                        int sampleX = GetPointSampleCoordinate(coordinateX);

                        if (sampleY >= sourceTop && sampleY < sourceBottom && sampleX >= sourceLeft && sampleX < sourceRight)
                        {
                            *dest = GetPointUnchecked(source, sampleX - sourceLeft, sampleY - sourceTop);
                        }
                    }
                    else // Bilinear or Bicubic
                    {
                        double sampleY = coordinateY.ToDouble();
                        int sampleYInt = (int)sampleY;

                        double sampleX = coordinateX.ToDouble();
                        int sampleXInt = (int)sampleX;

                        if (sampleYInt >= sourceTop && sampleYInt < sourceBottom && sampleXInt >= sourceLeft && sampleXInt < sourceRight)
                        {
                            double sampleYFrac = sampleY - Math.Floor(sampleY);
                            double sampleXFrac = sampleX - Math.Floor(sampleX);

                            switch (method)
                            {
                                case InterpolationMethod.Bilinear:
                                    *dest = GetResampledValueBilinear(source,
                                                                      sampleXInt - sourceLeft,
                                                                      sampleYInt - sourceTop,
                                                                      sampleXFrac,
                                                                      sampleYFrac);
                                    break;
                                case InterpolationMethod.Bicubic:
                                    *dest = GetResampledValueBicubic(source,
                                                                     sampleXInt - sourceLeft,
                                                                     sampleYInt - sourceTop,
                                                                     sampleXFrac,
                                                                     sampleYFrac);
                                    break;
                                case InterpolationMethod.PointSampling:
                                default:
                                    throw new InvalidOperationException($"Unsupported method value: {method}.");
                            }
                        }
                    }

                    dest += destination->colBytes;
                }
            }

            return PSError.noErr;
        }

        private static class ResamplingHelpers
        {
            // These methods are adapted from https://blog.demofox.org/2015/08/15/resizing-images-with-bicubic-interpolation/

            // t is a value that goes from 0 to 1 to interpolate in a C1 continuous way across uniformly sampled data points.
            // when t is 0, this will return B.  When t is 1, this will return C. In between values will return an interpolation
            // between B and C.  A and B are used to calculate slopes at the edges.
            public static double CubicHermite(double A, double B, double C, double D, double t)
            {
                double a = -A / 2.0f + (3.0f * B) / 2.0f - (3.0f * C) / 2.0f + D / 2.0f;
                double b = A - (5.0f * B) / 2.0f + 2.0f * C - D / 2.0f;
                double c = -A / 2.0f + C / 2.0f;
                double d = B;

                return a * t * t * t + b * t * t + c * t + d;
            }

            public static double Lerp(double A, double B, double t)
            {
                return A * (1.0f - t) + B * t;
            }
        }
    }
}
