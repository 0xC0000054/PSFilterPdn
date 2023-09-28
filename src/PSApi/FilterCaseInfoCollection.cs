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
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PSFilterLoad.PSApi
{
    [MessagePackObject]
    [MessagePackFormatter(typeof(Formatter))]
    internal sealed class FilterCaseInfoCollection : ReadOnlyCollection<FilterCaseInfo>
    {
        public FilterCaseInfoCollection(IList<FilterCaseInfo> list) : base(list)
        {
        }

        public FilterCaseInfo this[FilterCase filterCase] => this[(int)filterCase - 1];

        private sealed class Formatter : IMessagePackFormatter<FilterCaseInfoCollection?>
        {
            public FilterCaseInfoCollection? Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                if (reader.TryReadNil())
                {
                    return null;
                }

                options.Security.DepthStep(ref reader);

                IList<FilterCaseInfo> items = options.Resolver.GetFormatterWithVerify<IList<FilterCaseInfo>>().Deserialize(ref reader,
                                                                                                                           options);
                reader.Depth--;

                return new FilterCaseInfoCollection(items);
            }

            public void Serialize(ref MessagePackWriter writer, FilterCaseInfoCollection? value, MessagePackSerializerOptions options)
            {
                if (value is null)
                {
                    writer.WriteNil();
                    return;
                }

                options.Resolver.GetFormatterWithVerify<IList<FilterCaseInfo>>().Serialize(ref writer, value.Items, options);
            }
        }
    }
}
