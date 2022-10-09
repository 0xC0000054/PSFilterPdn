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

namespace PSFilterLoad.PSApi.ColorConversion
{
    internal sealed class RGB
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RGB"/> class.
        /// </summary>
        /// <param name="red">The red component in the range of [0, 1].</param>
        /// <param name="green">The green component in the range of [0, 1].</param>
        /// <param name="blue">The blue component in the range of [0, 1].</param>
        public RGB(double red, double green, double blue)
        {
            Red = red;
            Green = green;
            Blue = blue;
        }

        /// <summary>
        /// Gets the red component.
        /// </summary>
        /// <value>
        /// The red component.
        /// </value>
        public double Red
        {
            get;
        }

        /// <summary>
        /// Gets the green component.
        /// </summary>
        /// <value>
        /// The green component.
        /// </value>
        public double Green
        {
            get;
        }

        /// <summary>
        /// Gets the blue component.
        /// </summary>
        /// <value>
        /// The blue component.
        /// </value>
        public double Blue
        {
            get;
        }

        /// <summary>
        /// Gets the luminance intensity of the RGB color channels.
        /// </summary>
        /// <param name="maxChannelValue">The maximum channel value.</param>
        /// <returns>A value in the range of [0, 1] inclusive.</returns>
        public double GetIntensity(int maxChannelValue)
        {
            return ColorConverter.GetRGBIntensity(Red, Green, Blue, maxChannelValue);
        }
    }
}
