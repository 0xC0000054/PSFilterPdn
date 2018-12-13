/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2018 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

namespace PSFilterLoad.PSApi.ColorConversion
{
    internal sealed class Lab
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Lab"/> class.
        /// </summary>
        /// <param name="luminance">The luminance component in the range of [0, 100].</param>
        /// <param name="a">The a component in the range of [-128, 127].</param>
        /// <param name="b">The b component in the range of [-128, 127].</param>
        public Lab(double luminance, double a, double b)
        {
            L = luminance;
            A = a;
            B = b;
        }

        /// <summary>
        /// Gets the luminance component.
        /// </summary>
        /// <value>
        /// The luminance component.
        /// </value>
        public double L
        {
            get;
        }

        /// <summary>
        /// Gets the a component.
        /// </summary>
        /// <value>
        /// The a component.
        /// </value>
        public double A
        {
            get;
        }

        /// <summary>
        /// Gets the b component.
        /// </summary>
        /// <value>
        /// The b component.
        /// </value>
        public double B
        {
            get;
        }
    }
}
