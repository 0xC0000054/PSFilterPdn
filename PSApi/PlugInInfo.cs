﻿/* Adapted from PIGeneral.h
 * Copyright (c) 1992-6, Adobe Systems Incorporated.
 * All rights reserved.
*/

namespace PSFilterLoad.PSApi
{
    struct PlugInInfo
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
}