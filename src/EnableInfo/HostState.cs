/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2025 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

namespace PSFilterPdn.EnableInfo
{
    /// <summary>
    /// Represents the current state of the host application.
    /// </summary>
    internal sealed class HostState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HostState"/> class.
        /// </summary>
        public HostState()
        {
            HasMultipleLayers = false;
            HasSelection = false;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the current document has multiple layers.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the current document has multiple layers; otherwise, <c>false</c>.
        /// </value>
        public bool HasMultipleLayers
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the host has an active selection.
        /// </summary>
        /// <value>
        ///   <c>true</c> if host has an active selection; otherwise, <c>false</c>.
        /// </value>
        public bool HasSelection
        {
            get;
            set;
        }
    }
}
