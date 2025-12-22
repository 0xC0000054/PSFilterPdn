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

using MessagePack;
using MessagePack.Formatters;

namespace PSFilterLoad.PSApi
{
    [MessagePackObject]
    [MessagePackFormatter(typeof(Formatter))]
    internal sealed class UnitFloat
    {
        public UnitFloat(uint unit, double value)
        {
            Unit = unit;
            Value = value;
        }

        public uint Unit { get; }

        public double Value { get; }

        private sealed class Formatter : IMessagePackFormatter<UnitFloat>
        {
            public UnitFloat Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                options.Security.DepthStep(ref reader);

                uint unit = reader.ReadUInt32();
                double value = reader.ReadDouble();

                reader.Depth--;

                return new UnitFloat(unit, value);
            }

            public void Serialize(ref MessagePackWriter writer, UnitFloat value, MessagePackSerializerOptions options)
            {
                writer.Write(value.Unit);
                writer.Write(value.Value);
            }
        }
    }
}
