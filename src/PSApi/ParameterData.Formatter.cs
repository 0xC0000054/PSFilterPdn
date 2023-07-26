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

using MessagePack;
using MessagePack.Formatters;
using System.Collections.Generic;

#nullable enable

namespace PSFilterLoad.PSApi
{
    internal sealed partial class ParameterData
    {
        private sealed class Formatter : IMessagePackFormatter<ParameterData>
        {
            public ParameterData Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                IFormatterResolver resolver = options.Resolver;

                options.Security.DepthStep(ref reader);

                GlobalParameters globalParameters = resolver.GetFormatterWithVerify<GlobalParameters>().Deserialize(ref reader, options);
                Dictionary<uint, AETEValue>? scriptingData = resolver.GetFormatterWithVerify<Dictionary<uint, AETEValue>?>().Deserialize(ref reader,
                                                                                                                                         options);
                reader.Depth--;

                return new ParameterData(globalParameters, scriptingData);
            }

            public void Serialize(ref MessagePackWriter writer, ParameterData value, MessagePackSerializerOptions options)
            {
                IFormatterResolver resolver = options.Resolver;

                resolver.GetFormatterWithVerify<GlobalParameters>().Serialize(ref writer, value.GlobalParameters, options);
                resolver.GetFormatterWithVerify<Dictionary<uint, AETEValue>?>().Serialize(ref writer, value.ScriptingData, options);
            }
        }
    }
}