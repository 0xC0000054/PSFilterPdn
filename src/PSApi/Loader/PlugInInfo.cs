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

/* Adapted from PIGeneral.h
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

namespace PSFilterLoad.PSApi.Loader
{
#pragma warning disable 0649
    internal struct PlugInInfo
    {
        /// <summary>
        /// The version number of the interface supported.
        /// </summary>
        public short version;
        /// <summary>
        /// The sub-version number.
        /// </summary>
        public short subVersion;
        /// <summary>
        /// The plug-in's priority.
        /// </summary>
        public short priority;
        /// <summary>
        /// The size of the general info.
        /// </summary>
        public short generalInfoSize;
        /// <summary>
        /// The size of the type specific info.
        /// </summary>
        public short typeInfoSize;
        /// <summary>
        /// A bit mask indicating supported image modes.
        /// </summary>
        public short supportsMode;
        /// <summary>
        /// A required host if any.
        /// </summary>
        public uint requireHost;
    }
#pragma warning restore 0649
}