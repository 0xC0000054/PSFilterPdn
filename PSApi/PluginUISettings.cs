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

using System.Drawing;
using System.Runtime.Serialization;

namespace PSFilterLoad.PSApi
{
    /// <summary>
    /// Encapsulates the user interfaces settings for plug-in created dialogs.
    /// </summary>
    [DataContract]
    public sealed class PluginUISettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginUISettings"/> class.
        /// </summary>
        /// <param name="highDpi"><c>true</c> if the host is running in high DPI mode; otherwise, <c>false</c>.</param>
        /// <param name="colorPickerBackColor">The color picker background color.</param>
        /// <param name="colorPickerForeColor">The color picker foreground color.</param>
        internal PluginUISettings(bool highDpi, Color colorPickerBackColor, Color colorPickerForeColor)
        {
            ColorPickerBackColor = colorPickerBackColor;
            ColorPickerForeColor = colorPickerForeColor;
            HighDpi = highDpi;
        }

        /// <summary>
        /// Gets the color picker background color.
        /// </summary>
        /// <value>
        /// The color picker background color.
        /// </value>
        [DataMember]
        public Color ColorPickerBackColor
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the color picker foreground color.
        /// </summary>
        /// <value>
        /// The color picker foreground color.
        /// </value>
        [DataMember]
        public Color ColorPickerForeColor
        {
            get;
            private set;
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
