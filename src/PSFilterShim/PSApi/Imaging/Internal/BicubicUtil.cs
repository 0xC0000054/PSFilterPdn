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

// Portions adapted from:
/////////////////////////////////////////////////////////////////////////////////
// Paint.NET                                                                   //
// Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
// Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
// See License-pdn.txt for full licensing and attribution details.             //
//                                                                             //
/////////////////////////////////////////////////////////////////////////////////

#nullable enable

namespace PSFilterLoad.PSApi.Imaging.Internal
{
    internal static class BicubicUtil
    {
        // From https://blog.demofox.org/2015/08/15/resizing-images-with-bicubic-interpolation/
        // t is a value that goes from 0 to 1 to interpolate in a C1 continuous way across uniformly sampled data points.
        // when t is 0, this will return B.  When t is 1, this will return C. Inbetween values will return an interpolation
        // between B and C.  A and B are used to calculate slopes at the edges.
        internal static float CubicHermite(float A, float B, float C, float D, float t)
        {
            float a = -A / 2.0f + (3.0f * B) / 2.0f - (3.0f * C) / 2.0f + D / 2.0f;
            float b = A - (5.0f * B) / 2.0f + 2.0f * C - D / 2.0f;
            float c = -A / 2.0f + C / 2.0f;
            float d = B;

            return a * t * t * t + b * t * t + c * t + d;
        }

        // The following methods are used by the Paint.NET bicubic interpolation code:

        /// <summary>
        /// Implements R() as defined at http://astronomy.swin.edu.au/%7Epbourke/colour/bicubic/
        /// </summary>
        internal static double R(double x) => (CubeClamped(x + 2) - (4 * CubeClamped(x + 1)) + (6 * CubeClamped(x)) - (4 * CubeClamped(x - 1))) / 6;

        private static double CubeClamped(double x)
        {
            if (x > 0)
            {
                return x * x * x;
            }
            else
            {
                return 0;
            }
        }
    }
}
