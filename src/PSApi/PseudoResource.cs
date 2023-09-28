/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2023 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using MessagePack;
using MessagePack.Formatters;
using System.Buffers;

namespace PSFilterLoad.PSApi
{
    [MessagePackObject]
    [MessagePackFormatter(typeof(Formatter))]
    internal sealed class PseudoResource
    {
        private readonly uint key;
        private int index;
        private readonly byte[] data;

        public PseudoResource(uint key, int index, byte[] data)
        {
            this.key = key;
            this.index = index;
            this.data = data;
        }

        /// <summary>
        /// Gets the resource key.
        /// </summary>
        public uint Key => key;

        /// <summary>
        /// Gets the resource index.
        /// </summary>
        public int Index
        {
            get => index;
            set => index = value;
        }

        /// <summary>
        /// Gets the resource data.
        /// </summary>
        /// <returns></returns>
        public byte[] GetData()
        {
            return data;
        }

        public bool Equals(uint otherKey)
        {
            return key == otherKey;
        }

        public bool Equals(uint otherKey, int otherIndex)
        {
            return key == otherKey && index == otherIndex;
        }

        private sealed class Formatter : IMessagePackFormatter<PseudoResource>
        {
            public PseudoResource Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                options.Security.DepthStep(ref reader);

                uint key = reader.ReadUInt32();
                int index = reader.ReadInt32();
                byte[] data = reader.ReadBytes()!.Value.ToArray();

                reader.Depth--;

                return new PseudoResource(key, index, data);
            }

            public void Serialize(ref MessagePackWriter writer, PseudoResource value, MessagePackSerializerOptions options)
            {
                writer.Write(value.key);
                writer.Write(value.index);
                writer.Write(value.data);
            }
        }
    }
}
