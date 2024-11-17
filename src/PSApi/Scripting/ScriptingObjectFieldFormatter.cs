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

using CommunityToolkit.HighPerformance.Buffers;
using MessagePack;
using MessagePack.Formatters;
using PSFilterLoad.PSApi.PICA;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PSFilterLoad.PSApi
{
    /// <summary>
    /// The formatter used for 'object' fields in the scripting types.
    /// </summary>
    internal sealed class ScriptingObjectFieldFormatter : IMessagePackFormatter<object?>
    {
        /// <summary>
        /// The type code of the MessagePack extension that is used to identify the custom scripting types.
        /// </summary>
        /// <remarks>
        /// MessagePack-CSharp provides the following extension code ranges for library users:
        /// [0, 29] and [120, 127].
        /// See https://github.com/neuecc/MessagePack-CSharp#reserved-extension-types.
        /// </remarks>
        private const sbyte ExtensionTypeCode = 125;

        public static readonly IMessagePackFormatter<object?> Instance = new ScriptingObjectFieldFormatter();

        private static readonly HashSet<Type> ScriptingObjectExtensionTypes =
        [
            typeof(UnitFloat),
            typeof(EnumeratedValue),
            typeof(DescriptorSimpleReference),
            typeof(ReadOnlyCollection<ActionListItem>), // ActionDescriptorList
            typeof(Dictionary<uint, AETEValue>), // ActionDescriptorObject
            typeof(ReadOnlyCollection<ActionReferenceItem>), // ActionDescriptorReference
            typeof(ActionDescriptorZString),
            typeof(ActionListDescriptor),
            typeof(int[]),
        ];

        private ScriptingObjectFieldFormatter()
        {
        }

        private enum ScriptingObjectExtensionType : byte
        {
            UnitFloat = 0,
            EnumeratedValue,
            DescriptorSimpleReference,
            ActionDescriptorList,
            ActionDescriptorObject,
            ActionDescriptorReference,
            ActionDescriptorZString,
            ActionListDescriptor,
            Int32Array,
        }

        public object? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.TryReadNil())
            {
                return null;
            }

            if (reader.NextMessagePackType == MessagePackType.Extension)
            {
                MessagePackReader peekReader = reader.CreatePeekReader();

                ExtensionHeader header = peekReader.ReadExtensionFormatHeader();

                if (header.TypeCode == ExtensionTypeCode)
                {
                    reader = peekReader; // Replace the existing reader with the new one.

                    return ReadExtensionTypeData(ref reader, options);
                }
            }

            // Anything that is not our extension format should be a primitive type.
            return PrimitiveObjectFormatter.Instance.Deserialize(ref reader, options);
        }

        public void Serialize(ref MessagePackWriter writer, object? value, MessagePackSerializerOptions options)
        {
            if (value is null)
            {
                writer.WriteNil();
                return;
            }

            Type type = value.GetType();

            if (ScriptingObjectExtensionTypes.Contains(type))
            {
                using (ArrayPoolBufferWriter<byte> bufferWriter = new())
                {
                    MessagePackWriter scratchWriter = writer.Clone(bufferWriter);

                    WriteExtensionTypeData(ref scratchWriter, options, type, value);

                    scratchWriter.Flush();

                    ReadOnlySequence<byte> data = new(bufferWriter.WrittenMemory);
                    writer.WriteExtensionFormat(new ExtensionResult(ExtensionTypeCode, data));
                }
            }
            else
            {
                PrimitiveObjectFormatter.Instance.Serialize(ref writer, value, options);
            }
        }

        private static object ReadExtensionTypeData(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            ScriptingObjectExtensionType type = (ScriptingObjectExtensionType)reader.ReadByte();

            switch (type)
            {
                case ScriptingObjectExtensionType.UnitFloat:
                    return options.Resolver.GetFormatterWithVerify<UnitFloat>().Deserialize(ref reader, options);
                case ScriptingObjectExtensionType.EnumeratedValue:
                    return options.Resolver.GetFormatterWithVerify<EnumeratedValue>().Deserialize(ref reader, options);
                case ScriptingObjectExtensionType.DescriptorSimpleReference:
                    return options.Resolver.GetFormatterWithVerify<DescriptorSimpleReference>().Deserialize(ref reader, options);
                case ScriptingObjectExtensionType.ActionDescriptorList:
                    return options.Resolver.GetFormatterWithVerify<ReadOnlyCollection<ActionListItem>>().Deserialize(ref reader, options);
                case ScriptingObjectExtensionType.ActionDescriptorObject:
                    return options.Resolver.GetFormatterWithVerify<Dictionary<uint, AETEValue>>().Deserialize(ref reader, options);
                case ScriptingObjectExtensionType.ActionDescriptorReference:
                    return options.Resolver.GetFormatterWithVerify<ReadOnlyCollection<ActionReferenceItem>>().Deserialize(ref reader, options);
                case ScriptingObjectExtensionType.ActionDescriptorZString:
                    return options.Resolver.GetFormatterWithVerify<ActionDescriptorZString>().Deserialize(ref reader, options);
                case ScriptingObjectExtensionType.ActionListDescriptor:
                    return options.Resolver.GetFormatterWithVerify<ActionListDescriptor>().Deserialize(ref reader, options);
                case ScriptingObjectExtensionType.Int32Array:
                    return Int32ArrayFormatter.Instance.Deserialize(ref reader, options)!;
                default:
                    throw new MessagePackSerializationException($"Unsupported {nameof(ScriptingObjectFieldFormatter)} extension type code: {type}.");
            }
        }

        private static void WriteExtensionTypeData(ref MessagePackWriter scratchWriter,
                                                   MessagePackSerializerOptions options,
                                                   Type type,
                                                   object value)
        {
            if (type == typeof(UnitFloat))
            {
                scratchWriter.Write((byte)ScriptingObjectExtensionType.UnitFloat);
                options.Resolver.GetFormatterWithVerify<UnitFloat>().Serialize(ref scratchWriter, (UnitFloat)value, options);
            }
            else if (type == typeof(EnumeratedValue))
            {
                scratchWriter.Write((byte)ScriptingObjectExtensionType.EnumeratedValue);
                options.Resolver.GetFormatterWithVerify<EnumeratedValue>().Serialize(ref scratchWriter,
                                                                                     (EnumeratedValue)value,
                                                                                     options);
            }
            else if (type == typeof(DescriptorSimpleReference))
            {
                scratchWriter.Write((byte)ScriptingObjectExtensionType.DescriptorSimpleReference);
                options.Resolver.GetFormatterWithVerify<DescriptorSimpleReference>().Serialize(ref scratchWriter,
                                                                                               (DescriptorSimpleReference)value,
                                                                                               options);
            }
            else if (type == typeof(ReadOnlyCollection<ActionListItem>))
            {
                scratchWriter.Write((byte)ScriptingObjectExtensionType.ActionDescriptorList);
                options.Resolver.GetFormatterWithVerify<ReadOnlyCollection<ActionListItem>>().Serialize(ref scratchWriter,
                                                                                                        (ReadOnlyCollection<ActionListItem>)value,
                                                                                                        options);
            }
            else if (type == typeof(Dictionary<uint, AETEValue>))
            {
                scratchWriter.Write((byte)ScriptingObjectExtensionType.ActionDescriptorObject);
                options.Resolver.GetFormatterWithVerify<Dictionary<uint, AETEValue>>().Serialize(ref scratchWriter,
                                                                                                 (Dictionary<uint, AETEValue>)value,
                                                                                                 options);
            }
            else if (type == typeof(ReadOnlyCollection<ActionReferenceItem>))
            {
                scratchWriter.Write((byte)ScriptingObjectExtensionType.ActionDescriptorReference);
                options.Resolver.GetFormatterWithVerify<ReadOnlyCollection<ActionReferenceItem>>().Serialize(ref scratchWriter,
                                                                                                             (ReadOnlyCollection<ActionReferenceItem>)value,
                                                                                                             options);
            }
            else if (type == typeof(ActionDescriptorZString))
            {
                scratchWriter.Write((byte)ScriptingObjectExtensionType.ActionDescriptorZString);
                options.Resolver.GetFormatterWithVerify<ActionDescriptorZString>().Serialize(ref scratchWriter,
                                                                                             (ActionDescriptorZString)value,
                                                                                             options);
            }
            else if (type == typeof(ActionListDescriptor))
            {
                scratchWriter.Write((byte)ScriptingObjectExtensionType.ActionListDescriptor);
                options.Resolver.GetFormatterWithVerify<ActionListDescriptor>().Serialize(ref scratchWriter,
                                                                                          (ActionListDescriptor)value,
                                                                                          options);
            }
            else if (type == typeof(int[]))
            {
                scratchWriter.Write((byte)ScriptingObjectExtensionType.Int32Array);
                Int32ArrayFormatter.Instance.Serialize(ref scratchWriter, (int[])value, options);
            }
            else
            {
                throw new MessagePackSerializationException($"Unsupported {nameof(ScriptingObjectFieldFormatter)} extension type: {type}.");
            }
        }
    }
}
