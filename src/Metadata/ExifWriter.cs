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

using PaintDotNet.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PSFilterPdn.Metadata
{
    internal sealed class ExifWriter
    {
        private const int FirstIFDOffset = 8;

        private static readonly HashSet<ushort> supportedImageSectionTags = new()
        {
            // The tags related to storing offsets are included for reference,
            // but are not written to the EXIF blob.

            // Tags relating to image data structure
            256, // ImageWidth
            257, // ImageLength
            258, // BitsPerSample
            259, // Compression
            262, // PhotometricInterpretation
            274, // Orientation
            277, // SamplesPerPixel
            284, // PlanarConfiguration
            530, // YCbCrSubSampling
            531, // YCbCrPositioning
            282, // XResolution
            283, // YResolution
            296, // ResolutionUnit

            // Tags relating to recording offset
            //273, // StripOffsets
            //278, // RowsPerStrip
            //279, // StripByteCounts
            //513, // JPEGInterchangeFormat
            //514, // JPEGInterchangeFormatLength

            // Tags relating to image data characteristics
            301, // TransferFunction
            318, // WhitePoint
            319, // PrimaryChromaticities
            529, // YCbCrCoefficients
            532, // ReferenceBlackWhite

            // Other tags
            306, // DateTime
            270, // ImageDescription
            271, // Make
            272, // Model
            305, // Software
            315, // Artist
            33432 // Copyright
        };

        private readonly Dictionary<ExifSection, Dictionary<ushort, ExifValue>> metadata;

        public ExifWriter(IReadOnlyList<ExifPropertyItem> exifPropertyItems)
        {
            metadata = CreateTagDictionary(exifPropertyItems);
        }

        public byte[] CreateExifBlob()
        {
            IFDInfo ifdInfo = BuildIFDEntries();
            Dictionary<ExifSection, IFDEntryInfo> ifdEntries = ifdInfo.IFDEntries;

            byte[] exifBytes = new byte[checked((int)ifdInfo.EXIFDataLength)];

            using (MemoryStream stream = new(exifBytes))
            using (BinaryWriter writer = new(stream))
            {
                IFDEntryInfo imageInfo = ifdEntries[ExifSection.Image];
                IFDEntryInfo exifInfo = ifdEntries[ExifSection.Photo];

                writer.Write(TiffConstants.LittleEndianByteOrderMarker);
                writer.Write(TiffConstants.Signature);
                writer.Write((uint)imageInfo.StartOffset);

                WriteDirectory(writer, metadata[ExifSection.Image], imageInfo.IFDEntries, imageInfo.StartOffset);
                WriteDirectory(writer, metadata[ExifSection.Photo], exifInfo.IFDEntries, exifInfo.StartOffset);

                if (ifdEntries.TryGetValue(ExifSection.Interop, out IFDEntryInfo? interopInfo))
                {
                    WriteDirectory(writer, metadata[ExifSection.Interop], interopInfo.IFDEntries, interopInfo.StartOffset);
                }

                if (ifdEntries.TryGetValue(ExifSection.GpsInfo, out IFDEntryInfo? gpsInfo))
                {
                    WriteDirectory(writer, metadata[ExifSection.GpsInfo], gpsInfo.IFDEntries, gpsInfo.StartOffset);
                }
            }

            return exifBytes;
        }

        private static void WriteDirectory(BinaryWriter writer, Dictionary<ushort, ExifValue> tags, List<IFDEntry> entries, long ifdOffset)
        {
            writer.BaseStream.Position = ifdOffset;

            long nextIFDPointerOffset = ifdOffset + sizeof(ushort) + ((long)entries.Count * IFDEntry.SizeOf);

            writer.Write((ushort)entries.Count);

            foreach (IFDEntry entry in entries.OrderBy(e => e.Tag))
            {
                entry.Write(writer);

                if (!ExifValueTypeUtil.ValueFitsInOffsetField(entry.Type, entry.Count))
                {
                    long oldPosition = writer.BaseStream.Position;

                    writer.BaseStream.Position = entry.Offset;

                    writer.Write(tags[entry.Tag].Data.AsArrayOrToArray());

                    writer.BaseStream.Position = oldPosition;
                }
            }

            writer.BaseStream.Position = nextIFDPointerOffset;
            // There is only one IFD in this directory.
            writer.Write(0);
        }

        private IFDInfo BuildIFDEntries()
        {
            Dictionary<ushort, ExifValue> imageMetadata = metadata[ExifSection.Image];
            Dictionary<ushort, ExifValue> exifMetadata = metadata[ExifSection.Photo];

            // Add placeholders for the sub-IFD tags.
            imageMetadata.Add(
                ExifPropertyKeys.Image.ExifTag.Path.TagID,
                new ExifValue(ExifValueType.Long,
                              new byte[sizeof(uint)]));

            if (metadata.ContainsKey(ExifSection.GpsInfo))
            {
                imageMetadata.Add(
                ExifPropertyKeys.Image.GPSTag.Path.TagID,
                new ExifValue(ExifValueType.Long,
                              new byte[sizeof(uint)]));
            }

            if (metadata.ContainsKey(ExifSection.Interop))
            {
                exifMetadata.Add(
                    ExifPropertyKeys.Photo.InteroperabilityTag.Path.TagID,
                    new ExifValue(ExifValueType.Long,
                                  new byte[sizeof(uint)]));
            }

            return CalculateSectionOffsets();
        }

        private IFDInfo CalculateSectionOffsets()
        {
            IFDEntryInfo imageIFDInfo = CreateIFDList(metadata[ExifSection.Image], FirstIFDOffset);
            IFDEntryInfo exifIFDInfo = CreateIFDList(metadata[ExifSection.Photo], imageIFDInfo.NextAvailableOffset);
            IFDEntryInfo? interopIFDInfo = null;
            IFDEntryInfo? gpsIFDInfo = null;

            UpdateSubIFDOffset(ref imageIFDInfo,
                               ExifPropertyKeys.Image.ExifTag.Path.TagID,
                               (uint)exifIFDInfo.StartOffset);

            if (metadata.TryGetValue(ExifSection.Interop, out Dictionary<ushort, ExifValue>? interopSection))
            {
                interopIFDInfo = CreateIFDList(interopSection, exifIFDInfo.NextAvailableOffset);

                UpdateSubIFDOffset(ref exifIFDInfo,
                                   ExifPropertyKeys.Photo.InteroperabilityTag.Path.TagID,
                                   (uint)interopIFDInfo.StartOffset);
            }

            if (metadata.TryGetValue(ExifSection.GpsInfo, out Dictionary<ushort, ExifValue>? gpsSection))
            {
                long startOffset = interopIFDInfo?.NextAvailableOffset ?? exifIFDInfo.NextAvailableOffset;
                gpsIFDInfo = CreateIFDList(gpsSection, startOffset);

                UpdateSubIFDOffset(ref imageIFDInfo,
                                   ExifPropertyKeys.Image.GPSTag.Path.TagID,
                                   (uint)gpsIFDInfo.StartOffset);
            }

            return CreateIFDInfo(imageIFDInfo, exifIFDInfo, interopIFDInfo, gpsIFDInfo);
        }

        private static void UpdateSubIFDOffset(ref IFDEntryInfo ifdInfo, ushort tagId, uint newOffset)
        {
            int index = ifdInfo.IFDEntries.FindIndex(i => i.Tag == tagId);

            if (index != -1)
            {
                ifdInfo.IFDEntries[index] = new IFDEntry(tagId, ExifValueType.Long, 1, newOffset);
            }
        }

        private static IFDInfo CreateIFDInfo(
            IFDEntryInfo imageIFDInfo,
            IFDEntryInfo exifIFDInfo,
            IFDEntryInfo? interopIFDInfo,
            IFDEntryInfo? gpsIFDInfo)
        {
            Dictionary<ExifSection, IFDEntryInfo> entries = new()
            {
                { ExifSection.Image, imageIFDInfo },
                { ExifSection.Photo, exifIFDInfo }
            };

            long dataLength = exifIFDInfo.NextAvailableOffset;

            if (interopIFDInfo != null)
            {
                entries.Add(ExifSection.Interop, interopIFDInfo);
                dataLength = interopIFDInfo.NextAvailableOffset;
            }

            if (gpsIFDInfo != null)
            {
                entries.Add(ExifSection.GpsInfo, gpsIFDInfo);
                dataLength = gpsIFDInfo.NextAvailableOffset;
            }

            return new IFDInfo(entries, dataLength);
        }

        private static IFDEntryInfo CreateIFDList(Dictionary<ushort, ExifValue> tags, long startOffset)
        {
            List<IFDEntry> ifdEntries = new(tags.Count);

            // Leave room for the tag count, tags and next IFD offset.
            long ifdDataOffset = startOffset + sizeof(ushort) + ((long)tags.Count * IFDEntry.SizeOf) + sizeof(uint);

            foreach (KeyValuePair<ushort, ExifValue> item in tags.OrderBy(i => i.Key))
            {
                ushort tagID = item.Key;
                ExifValue entry = item.Value;

                uint lengthInBytes = (uint)entry.Data.Count;

                uint count;
                switch (entry.Type)
                {
                    case ExifValueType.Byte:
                    case ExifValueType.Ascii:
                    case ExifValueType.SByte:
                    case ExifValueType.Undefined:
                        count = lengthInBytes;
                        break;
                    case ExifValueType.Short:
                    case ExifValueType.SShort:
                        count = lengthInBytes / 2;
                        break;
                    case ExifValueType.Long:
                    case ExifValueType.SLong:
                    case ExifValueType.Float:
                    case ExifValueType.Ifd:
                        count = lengthInBytes / 4;
                        break;
                    case ExifValueType.Rational:
                    case ExifValueType.SRational:
                    case ExifValueType.Double:
                        count = lengthInBytes / 8;
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported { nameof(ExifValueType) }: { entry.Type }.");
                }

                if (ExifValueTypeUtil.ValueFitsInOffsetField(entry.Type, count))
                {
                    uint packedOffset = 0;

                    // Some applications may write EXIF fields with a count of zero.
                    // See https://github.com/0xC0000054/pdn-webp/issues/6.
                    if (count > 0)
                    {
                        IReadOnlyList<byte> data = entry.Data;

                        // The data is always in little-endian byte order.
                        switch (data.Count)
                        {
                            case 1:
                                packedOffset |= data[0];
                                break;
                            case 2:
                                packedOffset |= data[0];
                                packedOffset |= (uint)data[1] << 8;
                                break;
                            case 3:
                                packedOffset |= data[0];
                                packedOffset |= (uint)data[1] << 8;
                                packedOffset |= (uint)data[2] << 16;
                                break;
                            case 4:
                                packedOffset |= data[0];
                                packedOffset |= (uint)data[1] << 8;
                                packedOffset |= (uint)data[2] << 16;
                                packedOffset |= (uint)data[3] << 24;
                                break;
                            default:
                                throw new InvalidOperationException("data.Count must be in the range of [1-4].");
                        }
                    }

                    ifdEntries.Add(new IFDEntry(tagID, entry.Type, count, packedOffset));
                }
                else
                {
                    ifdEntries.Add(new IFDEntry(tagID, entry.Type, count, (uint)ifdDataOffset));
                    ifdDataOffset += lengthInBytes;

                    // The IFD offsets must begin on a WORD boundary.
                    if ((ifdDataOffset & 1) == 1)
                    {
                        ifdDataOffset++;
                    }
                }
            }

            return new IFDEntryInfo(ifdEntries, startOffset, ifdDataOffset);
        }

        private static Dictionary<ExifSection, Dictionary<ushort, ExifValue>> CreateTagDictionary(
            IReadOnlyList<ExifPropertyItem> exifPropertyItems)
        {
            Dictionary<ExifSection, Dictionary<ushort, ExifValue>> metadataEntries = new()
            {
                {
                    ExifSection.Image,
                    new Dictionary<ushort, ExifValue>()
                },
                {
                    ExifSection.Photo,
                    new Dictionary<ushort, ExifValue>()
                }
            };

            ExifColorSpace exifColorSpace = ExifColorSpace.Srgb;
            bool setColorSpace = false;

            foreach (ExifPropertyItem item in exifPropertyItems)
            {
                ExifPropertyPath key = item.Path;
                ExifValue value = item.Value;

                ExifSection section = key.Section;

                if (section == ExifSection.Image)
                {
                    if (key == ExifPropertyKeys.Image.InterColorProfile.Path)
                    {
                        exifColorSpace = ExifColorSpace.Uncalibrated;
                        setColorSpace = true;
                        continue;
                    }
                    else if (!supportedImageSectionTags.Contains(key.TagID))
                    {
                        continue;
                    }
                }
                else if (key == ExifPropertyKeys.Photo.ColorSpace.Path)
                {
                    if (!setColorSpace)
                    {
                        exifColorSpace = (ExifColorSpace)ExifConverter.DecodeShort(value.Data);
                        setColorSpace = true;
                    }
                    continue;
                }
                else if (key.Section == ExifSection.Interop)
                {
                    // The Interop values will be written based on the color space of the image.
                    continue;
                }

                if (metadataEntries.TryGetValue(section, out Dictionary<ushort, ExifValue>? values))
                {
                    values.TryAdd(key.TagID, value);
                }
                else
                {
                    metadataEntries.Add(section, new Dictionary<ushort, ExifValue>
                    {
                        { key.TagID, value }
                    });
                }
            }

            // Add the EXIF color space tag.
            // AdobeRGB is not an official EXIF color space value, so it is treated as uncalibrated.
            metadataEntries[ExifSection.Photo].Add(ExifPropertyKeys.Photo.ColorSpace.Path.TagID,
                                                   ExifConverter.EncodeShort(exifColorSpace == ExifColorSpace.AdobeRgb ?
                                                                             (ushort)ExifColorSpace.Uncalibrated :
                                                                             (ushort)exifColorSpace));

            // Add the Interoperability IFD tags for sRGB or Adobe RGB images.
            if (exifColorSpace == ExifColorSpace.Srgb || exifColorSpace == ExifColorSpace.AdobeRgb)
            {
                byte[] interoperabilityIndexData = exifColorSpace switch
                {
                    ExifColorSpace.Srgb => new byte[] { (byte)'R', (byte)'9', (byte)'8', 0 },
                    ExifColorSpace.AdobeRgb => new byte[] { (byte)'R', (byte)'0', (byte)'3', 0 },
                    _ => throw new InvalidOperationException("Unsupported ExifColorSpace value."),
                };
                Dictionary<ushort, ExifValue> interopSection = new()
                {
                    {
                        ExifPropertyKeys.Interop.InteroperabilityIndex.Path.TagID,
                        new ExifValue(ExifValueType.Ascii, interoperabilityIndexData)
                    },
                    {
                        ExifPropertyKeys.Interop.InteroperabilityVersion.Path.TagID,
                        new ExifValue(ExifValueType.Undefined, "0100"u8.ToArray())
                    }
                };

                metadataEntries.Add(ExifSection.Interop, interopSection);
            }

            AddVersionEntries(ref metadataEntries);

            return metadataEntries;
        }

        private static void AddVersionEntries(ref Dictionary<ExifSection, Dictionary<ushort, ExifValue>> metadataEntries)
        {
            if (metadataEntries.TryGetValue(ExifSection.Photo, out Dictionary<ushort, ExifValue>? exifItems))
            {
                if (!exifItems.ContainsKey(ExifPropertyKeys.Photo.ExifVersion.Path.TagID))
                {
#pragma warning disable IDE0230 // Use UTF-8 string literal
                    exifItems.Add(
                        ExifPropertyKeys.Photo.ExifVersion.Path.TagID,
                        new ExifValue(ExifValueType.Undefined,
                                      new byte[] { (byte)'0', (byte)'2', (byte)'3', (byte)'0' }));
#pragma warning restore IDE0230 // Use UTF-8 string literal
                }
            }

            if (metadataEntries.TryGetValue(ExifSection.GpsInfo, out Dictionary<ushort, ExifValue>? gpsItems))
            {
                if (!gpsItems.ContainsKey(ExifPropertyKeys.GpsInfo.GPSVersionID.Path.TagID))
                {
                    gpsItems.Add(
                        ExifPropertyKeys.GpsInfo.GPSVersionID.Path.TagID,
                        new ExifValue(ExifValueType.Byte,
                                      new byte[] { 2, 3, 0, 0 }));
                }
            }
        }

        private sealed class IFDEntryInfo
        {
            public IFDEntryInfo(List<IFDEntry> ifdEntries, long startOffset, long nextAvailableOffset)
            {
                ArgumentNullException.ThrowIfNull(ifdEntries, nameof(ifdEntries));

                IFDEntries = ifdEntries;
                StartOffset = startOffset;
                NextAvailableOffset = nextAvailableOffset;
            }

            public List<IFDEntry> IFDEntries { get; }

            public long StartOffset { get; }

            public long NextAvailableOffset { get; }
        }

        private sealed class IFDInfo
        {
            public IFDInfo(Dictionary<ExifSection, IFDEntryInfo> entries, long exifDataLength)
            {
                ArgumentNullException.ThrowIfNull(entries, nameof(entries));

                IFDEntries = entries;
                EXIFDataLength = exifDataLength;
            }

            public Dictionary<ExifSection, IFDEntryInfo> IFDEntries { get; }

            public long EXIFDataLength { get; }
        }
    }
}
