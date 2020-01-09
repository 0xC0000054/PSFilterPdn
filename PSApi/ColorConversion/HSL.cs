/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2020 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

namespace PSFilterLoad.PSApi.ColorConversion
{
    internal sealed class HSL
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HSL"/> class.
        /// </summary>
        /// <param name="hue">The hue component in the range of [0, 1].</param>
        /// <param name="saturation">The saturation component in the range of [0, 1].</param>
        /// <param name="luminance">The luminance component in the range of [0, 1].</param>
        public HSL(double hue, double saturation, double luminance)
        {
            Hue = hue;
            Saturation = saturation;
            Luminance = luminance;
        }

        /// <summary>
        /// Gets the hue component in the range of [0, 1].
        /// </summary>
        /// <value>
        /// The hue component.
        /// </value>
        public double Hue
        {
            get;
        }

        /// <summary>
        /// Gets the saturation component.
        /// </summary>
        /// <value>
        /// The saturation component.
        /// </value>
        public double Saturation
        {
            get;
        }

        /// <summary>
        /// Gets the luminance component.
        /// </summary>
        /// <value>
        /// The luminance component.
        /// </value>
        public double Luminance
        {
            get;
        }
    }
}
