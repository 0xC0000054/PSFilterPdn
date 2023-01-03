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

using System.Runtime.Serialization;

namespace PSFilterLoad.PSApi
{
    /// <summary>
    /// Encapsulates the user interfaces settings for plug-in created dialogs.
    /// </summary>
    [DataContract]
    internal sealed class PluginUISettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginUISettings"/> class.
        /// </summary>
        /// <param name="highDpi"><c>true</c> if the host is running in high DPI mode; otherwise, <c>false</c>.</param>
        internal PluginUISettings(bool highDpi)
        {
            HighDpi = highDpi;
        }

        /// <summary>
        /// Gets a value indicating whether the host is running in high DPI mode.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the host is running in high DPI mode; otherwise, <c>false</c>.
        /// </value>
        [DataMember]
        public bool HighDpi
        {
            get;
            private set;
        }
    }
}
