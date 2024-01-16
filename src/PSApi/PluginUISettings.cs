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

using MessagePack;
using MessagePack.Formatters;

namespace PSFilterLoad.PSApi
{
    /// <summary>
    /// Encapsulates the user interfaces settings for plug-in created dialogs.
    /// </summary>
    [MessagePackObject]
    [MessagePackFormatter(typeof(Formatter))]
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
        public bool HighDpi
        {
            get;
            private set;
        }

        private sealed class Formatter : IMessagePackFormatter<PluginUISettings?>
        {
            public PluginUISettings? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                if (reader.TryReadNil())
                {
                    return null;
                }

                options.Security.DepthStep(ref reader);

                bool highDpi = reader.ReadBoolean();

                reader.Depth--;

                return new PluginUISettings(highDpi);
            }

            public void Serialize(ref MessagePackWriter writer, PluginUISettings? value, MessagePackSerializerOptions options)
            {
                if (value is null)
                {
                    writer.WriteNil();
                    return;
                }

                writer.Write(value.HighDpi);
            }
        }
    }
}
