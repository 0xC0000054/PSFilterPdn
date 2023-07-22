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

using MessagePack;
using MessagePack.Formatters;
using System.Collections.Generic;

#nullable enable

namespace PSFilterLoad.PSApi
{
    [MessagePackObject]
    [MessagePackFormatter(typeof(Formatter))]
    internal sealed class AETEData
    {
        private readonly Dictionary<uint, short> flagList;

        internal AETEData(Dictionary<uint, short> aeteParameterFlags)
        {
            flagList = aeteParameterFlags;
        }

        public bool TryGetParameterFlags(uint key, out short flags)
        {
            return flagList.TryGetValue(key, out flags);
        }

        private sealed class Formatter : IMessagePackFormatter<AETEData?>
        {
            public AETEData? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                if (reader.TryReadNil())
                {
                    return null;
                }

                options.Security.DepthStep(ref reader);

                AETEData data = new(options.Resolver.GetFormatterWithVerify<Dictionary<uint, short>>().Deserialize(ref reader, options));

                reader.Depth--;

                return data;
            }

            public void Serialize(ref MessagePackWriter writer, AETEData? value, MessagePackSerializerOptions options)
            {
                if (value is null)
                {
                    writer.WriteNil();
                    return;
                }

                options.Resolver.GetFormatterWithVerify<Dictionary<uint, short>>().Serialize(ref writer, value.flagList, options);
            }
        }
    }
}
