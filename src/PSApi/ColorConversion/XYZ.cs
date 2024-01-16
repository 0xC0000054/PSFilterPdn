/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2024 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

namespace PSFilterLoad.PSApi.ColorConversion
{
    internal sealed class XYZ
    {
        /// <summary>
        /// The D50 reference white point.
        /// </summary>
        public static readonly XYZ D50 = new(0.9642, 1.0, 0.8252);

        /// <summary>
        /// Initializes a new instance of the <see cref="XYZ"/> class.
        /// </summary>
        /// <param name="x">The x component in the range of [0, 1].</param>
        /// <param name="y">The y component in the range of [0, 1].</param>
        /// <param name="z">The z component in the range of [0, 1].</param>
        public XYZ(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>
        /// Gets the x component.
        /// </summary>
        /// <value>
        /// The x component.
        /// </value>
        public double X
        {
            get;
        }

        /// <summary>
        /// Gets the y component.
        /// </summary>
        /// <value>
        /// The y component.
        /// </value>
        public double Y
        {
            get;
        }

        /// <summary>
        /// Gets the z component.
        /// </summary>
        /// <value>
        /// The z component.
        /// </value>
        public double Z
        {
            get;
        }
    }
}
