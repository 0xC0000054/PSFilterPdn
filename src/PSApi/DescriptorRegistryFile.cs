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

using CommunityToolkit.HighPerformance.Buffers;
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
            try
            {
                bool shouldDeleteOldVersion = false;

                using (FileStream fs = new(path, FileMode.Open, FileAccess.Read))
                {
                    if (DescriptorRegistryFileHeader.TryCreate(fs, out DescriptorRegistryFileHeader header))
                    {
                        if (header.FileVersion == 5)
                        {
                            long dataLength = fs.Length - fs.Position;

                            if (dataLength > int.MaxValue)
                            {
                                throw new IOException("The descriptor registry data is larger than 2GB.");
                            }

                            using (MemoryOwner<byte> memoryOwner = MemoryOwner<byte>.Allocate((int)dataLength))
                            {
                                fs.ReadExactly(memoryOwner.Span);

                                var values = MessagePackSerializerUtil.Deserialize<Dictionary<string, Dictionary<uint, AETEValue>>>(memoryOwner.Memory,
                                                                                                                                    MessagePackResolver.Options);
                                return new DescriptorRegistryValues(values);
                            }
                        }
                        else
                        {
                            // The descriptor registry file is an unsupported version, delete it.
                            shouldDeleteOldVersion = true;
                        }
                    }
                    else
                    {
                        // The descriptor registry file is an unsupported version, delete it.
                        shouldDeleteOldVersion = true;
                    }
                }

                if (shouldDeleteOldVersion)
                {
                    File.Delete(path);
                }
            }
            catch (DirectoryNotFoundException)
            {
                // The Paint.NET user files folder does not exist.
            }
            catch (FileNotFoundException)
            {
                // This file would only exist if a plugin has persisted settings.
            }

            return new DescriptorRegistryValues();
        }

        public static void Save(string path, DescriptorRegistryValues values)
        {
            if (values.Dirty)
            {
                using (FileStream fs = new(path, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    new DescriptorRegistryFileHeader().Save(fs);

                    using (ArrayPoolBufferWriter<byte> bufferWriter = new())
                    {
                        MessagePackSerializerUtil.Serialize(bufferWriter,
                                                            values.GetPersistedValuesReadOnly(),
                                                            MessagePackResolver.Options);

                        fs.Write(bufferWriter.WrittenSpan);
                    }
                }

                values.Dirty = false;
            }
        }

        private sealed class DescriptorRegistryFileHeader
        {
            public DescriptorRegistryFileHeader()
            {
                FileVersion = 5;
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
