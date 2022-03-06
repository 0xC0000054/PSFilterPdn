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

using System;
using System.IO;
using System.Security;

namespace PSFilterLoad.PSApi
{
    internal static class PEFile
    {
        private const ushort IMAGE_DOS_SIGNATURE = 0x5A4D; // MZ
        private const uint IMAGE_NT_SIGNATURE = 0x00004550; // PE00
        private const ushort IMAGE_FILE_MACHINE_I386 = 0x14C;
        private const ushort IMAGE_FILE_MACHINE_AMD64 = 0x8664;
        private const ushort IMAGE_FILE_MACHINE_ARM = 0x01C0;
        private const ushort IMAGE_FILE_MACHINE_ARM64 = 0xAA64;
        private const int NTSignatureOffsetLocation = 0x3C;

        /// <summary>
        /// Gets the processor architecture that the module was built for.
        /// </summary>
        /// <param name="fileName">The file name to check.</param>
        /// <returns>The processor architecture of the module.</returns>
        internal static ProcessorArchitecture GetProcessorArchitecture(string fileName)
        {
            ProcessorArchitecture architecture = ProcessorArchitecture.Unknown;

            FileStream stream = null;

            try
            {
                stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);

                using (EndianBinaryReader reader = new EndianBinaryReader(stream, Endianess.Little))
                {
                    stream = null;

                    architecture = GetProcessorArchitectureImpl(reader);
                }
            }
            catch (ArgumentException)
            {
            }
            catch (IOException)
            {
            }
            catch (NotSupportedException)
            {
            }
            catch (SecurityException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            finally
            {
                stream?.Dispose();
            }

            return architecture;
        }

        private static ProcessorArchitecture GetProcessorArchitectureImpl(EndianBinaryReader reader)
        {
            ProcessorArchitecture architecture = ProcessorArchitecture.Unknown;

            ushort dosSignature = reader.ReadUInt16();
            if (dosSignature == IMAGE_DOS_SIGNATURE)
            {
                reader.Position = NTSignatureOffsetLocation;

                uint ntSignatureOffset = reader.ReadUInt32();

                reader.Position = ntSignatureOffset;

                uint ntSignature = reader.ReadUInt32();
                if (ntSignature == IMAGE_NT_SIGNATURE)
                {
                    ushort machine = reader.ReadUInt16();

                    switch (machine)
                    {
                        case IMAGE_FILE_MACHINE_I386:
                            architecture = ProcessorArchitecture.X86;
                            break;
                        case IMAGE_FILE_MACHINE_AMD64:
                            architecture = ProcessorArchitecture.X64;
                            break;
                        case IMAGE_FILE_MACHINE_ARM:
                            architecture = ProcessorArchitecture.Arm;
                            break;
                        case IMAGE_FILE_MACHINE_ARM64:
                            architecture = ProcessorArchitecture.Arm64;
                            break;
                    }
                }
            }

            return architecture;
        }
    }
}
