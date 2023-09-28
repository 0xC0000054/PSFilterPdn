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

using PSFilterPdn;
using System;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi.Loader
{
    internal static class PEFile
    {
        private const ushort IMAGE_DOS_SIGNATURE = 0x5A4D; // MZ
        private const uint IMAGE_NT_SIGNATURE = 0x00004550; // PE00
        private const ushort IMAGE_FILE_MACHINE_I386 = 0x14C;
        private const ushort IMAGE_FILE_MACHINE_AMD64 = 0x8664;
        private const ushort IMAGE_FILE_MACHINE_ARM64 = 0xAA64;
        private const int NTSignatureOffsetLocation = 0x3C;

        /// <summary>
        /// Gets the processor architecture that the module was built for.
        /// </summary>
        /// <param name="fileName">The file name to check.</param>
        /// <returns>The processor architecture of the module.</returns>
        internal static Architecture GetProcessorArchitecture(string fileName)
        {
            Architecture architecture;

            FileStream? stream = null;

            try
            {
                // Prevent the FileStream from creating a buffer because the EndianBinaryReader will perform its own buffering.
                stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1);

                using (EndianBinaryReader reader = new(stream, Endianess.Little))
                {
                    stream = null;

                    architecture = GetProcessorArchitectureImpl(reader);
                }
            }
            finally
            {
                stream?.Dispose();
            }

            return architecture;
        }

        private static Architecture GetProcessorArchitectureImpl(EndianBinaryReader reader)
        {
            ushort dosSignature = reader.ReadUInt16();
            if (dosSignature != IMAGE_DOS_SIGNATURE)
            {
                throw new BadImageFormatException("Unknown file format.");
            }

            reader.Position = NTSignatureOffsetLocation;

            uint ntSignatureOffset = reader.ReadUInt32();

            reader.Position = ntSignatureOffset;

            uint ntSignature = reader.ReadUInt32();
            if (ntSignature != IMAGE_NT_SIGNATURE)
            {
                throw new BadImageFormatException("Invalid PE header.");
            }

            ushort machine = reader.ReadUInt16();

            switch (machine)
            {
                case IMAGE_FILE_MACHINE_I386:
                    return Architecture.X86;
                case IMAGE_FILE_MACHINE_AMD64:
                    return Architecture.X64;
                case IMAGE_FILE_MACHINE_ARM64:
                    return Architecture.Arm64;
                default:
                    throw new PlatformNotSupportedException(string.Format(CultureInfo.InvariantCulture,
                                                                          "Unsupported machine type: 0x{0:X4}",
                                                                          machine));
            }
        }
    }
}
