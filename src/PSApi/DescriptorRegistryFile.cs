﻿/////////////////////////////////////////////////////////////////////////////////
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

using PSFilterPdn;
using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

namespace PSFilterLoad.PSApi
{
    internal static class DescriptorRegistryFile
    {
        public static DescriptorRegistryValues Load(string path)
        {
            DescriptorRegistryValues registryValues = new();

            using (FileStream fs = new(path, FileMode.Open, FileAccess.Read))
            {
                if (DescriptorRegistryFileHeader.TryCreate(fs, out DescriptorRegistryFileHeader header))
                {
                    if (header.FileVersion == 4)
                    {
                        long dataLength = fs.Length - fs.Position;

                        byte[] data = new byte[dataLength];

                        fs.ReadExactly(data, 0, data.Length);

                        using (MemoryStream ms = new(data))
                        {
                            var values = DataContractSerializerUtil.Deserialize<Dictionary<string, Dictionary<uint, AETEValue>>>(ms);
                            registryValues = new DescriptorRegistryValues(values);
                        }
                    }
                }
            }

            return registryValues;
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
                        DataContractSerializerUtil.Serialize(ms, values.GetPersistedValuesReadOnly());

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
                FileVersion = 4;
            }

            private DescriptorRegistryFileHeader(int fileVersion)
            {
                FileVersion = fileVersion;
            }

            public int FileVersion { get; }

            // PFPR = PSFilterPdn registry
            private static ReadOnlySpan<byte> Signature => "PFPR"u8;

            [SkipLocalsInit]
            public static bool TryCreate(Stream stream, out DescriptorRegistryFileHeader header)
            {
                header = null;

                bool result = false;

                if (stream.Length > 8)
                {
                    Span<byte> headerBytes = stackalloc byte[8];

                    stream.ReadExactly(headerBytes);

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
