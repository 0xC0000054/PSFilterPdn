/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2019 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PaintDotNet;

namespace PSFilterLoad.PSApi
{
    /// <summary>
    /// Provides access to the images that a filter reads and writes.
    /// </summary>
    internal interface IFilterImageProvider
    {
        /// <summary>
        /// Gets the filter source image.
        /// </summary>
        /// <value>
        /// The filter source image.
        /// </value>
        Surface Source
        {
            get;
        }

        /// <summary>
        /// Gets the filter destination image.
        /// </summary>
        /// <value>
        /// The filter destination image.
        /// </value>
        Surface Destination
        {
            get;
        }

        /// <summary>
        /// Gets the filter mask image.
        /// </summary>
        /// <value>
        /// The filter mask image.
        /// </value>
        MaskSurface Mask
        {
            get;
        }
    }
}
