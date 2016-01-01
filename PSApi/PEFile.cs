/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2016 Nicholas Hayes
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
        private static uint ReadUInt32(Stream stream)
        {
            int byte1 = stream.ReadByte();
            if (byte1 == -1)
            {
                throw new EndOfStreamException();
            }
            
            int byte2 = stream.ReadByte();
            if (byte2 == -1)
            {
                throw new EndOfStreamException();
            }

            int byte3 = stream.ReadByte();
            if (byte3 == -1)
            {
                throw new EndOfStreamException();
            }

            int byte4 = stream.ReadByte();
            if (byte4 == -1)
            {
                throw new EndOfStreamException();
            }

            return (uint)(byte1 | (byte2 << 8) | (byte3 << 16) | (byte4 << 24));
        }

        private static ushort ReadUInt16(Stream stream)
        {
            int byte1 = stream.ReadByte();
            if (byte1 == -1)
            {
                throw new EndOfStreamException();
            }

            int byte2 = stream.ReadByte();
            if (byte2 == -1)
            {
                throw new EndOfStreamException();
            }

            return (ushort)(byte1 | (byte2 << 8));
        }

        private const ushort IMAGE_DOS_SIGNATURE = 0x5A4D; // MZ
        private const uint IMAGE_NT_SIGNATURE = 0x00004550; // PE00
        private const ushort IMAGE_FILE_MACHINE_I386 = 0x14C;
        private const ushort IMAGE_FILE_MACHINE_AMD64 = 0x8664;
        private const int NTSignatureOffsetLocation = 0x3C;

        internal enum ProcessorArchitecture
        { 
            Unknown = 0,
            X86, 
            X64
        }

        /// <summary>
        /// Gets the processor architecture that the module was built for.
        /// </summary>
        /// <param name="fileName">The file name to check.</param>
        /// <returns>The processor architecture of the module.</returns>
        internal static ProcessorArchitecture GetProcessorArchitecture(string fileName)
        {
            ProcessorArchitecture architecture = ProcessorArchitecture.Unknown;

            try
            {
                using (FileStream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    ushort dosSignature = ReadUInt16(stream);
                    if (dosSignature == IMAGE_DOS_SIGNATURE)
                    {
                        stream.Seek(NTSignatureOffsetLocation, SeekOrigin.Begin);

                        uint ntSignatureOffset = ReadUInt32(stream);

                        stream.Seek(ntSignatureOffset, SeekOrigin.Begin);

                        uint ntSignature = ReadUInt32(stream);
                        if (ntSignature == IMAGE_NT_SIGNATURE)
                        {
                            ushort machine = ReadUInt16(stream);

                            switch (machine)
                            { 
                                case IMAGE_FILE_MACHINE_I386:
                                    architecture = ProcessorArchitecture.X86;
                                    break;
                                case IMAGE_FILE_MACHINE_AMD64:
                                    architecture = ProcessorArchitecture.X64;
                                    break;
                            }
                        }
                    }
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

            return architecture;
        }
    }
}
