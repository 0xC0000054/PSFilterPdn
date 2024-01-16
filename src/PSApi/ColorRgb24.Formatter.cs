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
    internal readonly partial struct ColorRgb24
    {
        private sealed class Formatter : IMessagePackFormatter<ColorRgb24>
        {
            public ColorRgb24 Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                options.Security.DepthStep(ref reader);

                byte red = reader.ReadByte();
                byte green = reader.ReadByte();
                byte blue = reader.ReadByte();

                reader.Depth--;

                return new ColorRgb24(red, green, blue);
            }

            public void Serialize(ref MessagePackWriter writer, ColorRgb24 value, MessagePackSerializerOptions options)
            {
                writer.Write(value.R);
                writer.Write(value.G);
                writer.Write(value.B);
            }
        }
    }
}
