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

using PaintDotNet.IO;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace PSFilterLoad.PSApi
{
    internal static class DescriptorRegistryFile
    {
        public static DescriptorRegistryValues Losd(string path)
        {
            Dictionary<string, DescriptorRegistryItem> persistentItems = new(StringComparer.Ordinal);
            bool isOldFormat = false;

            using (FileStream fs = new(path, FileMode.Open, FileAccess.Read))
            {
                IDictionary<string, DescriptorRegistryItem> values = null;

                if (DescriptorRegistryFileHeader.TryCreate(fs, out DescriptorRegistryFileHeader header))
                {
                    if (header.FileVersion == 2)
                    {
                        long dataLength = fs.Length - fs.Position;

                        byte[] data = new byte[dataLength];

                        fs.ProperRead(data, 0, data.Length);

                        using (MemoryStream ms = new(data))
                        {
                            values = PSFilterPdn.DataContractSerializerUtil.Deserialize<Dictionary<string, DescriptorRegistryItem>>(ms);
                        }
                    }
                }

                if (values != null && values.Count > 0)
                {
                    foreach (KeyValuePair<string, DescriptorRegistryItem> item in values)
                    {
                        persistentItems.Add(item.Key, item.Value);
                    }
                }
            }

            return new DescriptorRegistryValues(persistentItems, isOldFormat);
        }

        public static void Save(string path, DescriptorRegistryValues values)
        {
            if (values.Dirty)
            {
                using (FileStream fs = new(path, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    new DescriptorRegistryFileHeader().Save(fs);

                    using (MemoryStream ms = new())
                    {
                        PSFilterPdn.DataContractSerializerUtil.Serialize(ms, values.GetPersistedValuesReadOnly());

                        fs.Write(ms.GetBuffer(), 0, (int)ms.Length);
                    }
                }

                values.Dirty = false;
            }
        }

        private sealed class DescriptorRegistryFileHeader
        {
            public DescriptorRegistryFileHeader()
            {
                FileVersion = 2;
            }

            private DescriptorRegistryFileHeader(int fileVersion)
            {
                FileVersion = fileVersion;
            }

            public int FileVersion { get; }

            // PFPR = PSFilterPdn registry
            private static ReadOnlySpan<byte> Signature => new byte[] { (byte)'P', (byte)'F', (byte)'P', (byte)'R' };

            [SkipLocalsInit]
            public static bool TryCreate(Stream stream, out DescriptorRegistryFileHeader header)
            {
                header = null;

                bool result = false;

                if (stream.Length > 8)
                {
                    Span<byte> headerBytes = stackalloc byte[8];

                    stream.ProperRead(headerBytes);

                    if (CheckHeaderSignature(headerBytes))
                    {
                        header = new DescriptorRegistryFileHeader(BinaryPrimitives.ReadInt32LittleEndian(headerBytes.Slice(4)));
                        result = true;
                    }
                }

                return result;
            }

            [SkipLocalsInit]
            public void Save(Stream stream)
            {
                Span<byte> headerBytes = stackalloc byte[8];

                Signature.CopyTo(headerBytes);
                BinaryPrimitives.WriteInt32LittleEndian(headerBytes.Slice(4), FileVersion);

                stream.Write(headerBytes);
            }

            private static bool CheckHeaderSignature(ReadOnlySpan<byte> bytes)
            {
                return bytes.Length >= Signature.Length && bytes.Slice(0, Signature.Length).SequenceEqual(Signature);
            }
        }
    }
}
