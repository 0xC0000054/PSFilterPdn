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

using MessagePack;
using MessagePack.Formatters;
using PSFilterLoad.PSApi;

namespace PSFilterPdn
{
    internal sealed class PSFilterShimResolver : IFormatterResolver
    {
        public static readonly IFormatterResolver Instance;
        public static readonly MessagePackSerializerOptions Options;

        static PSFilterShimResolver()
        {
            Instance = new PSFilterShimResolver();
            Options = new MessagePackSerializerOptions(Instance).WithCompression(MessagePackCompression.Lz4BlockArray);
        }

        private PSFilterShimResolver()
        {
        }

        public IMessagePackFormatter<T>? GetFormatter<T>() => FormatterCache<T>.Formatter;

        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T>? Formatter;

            static FormatterCache()
            {
                if (typeof(T) == typeof(PluginData))
                {
                    Formatter = (IMessagePackFormatter<T>)PSFilterShimPluginDataFormatter.Instance;
                }
                else
                {
                    Formatter = MessagePackResolver.Instance.GetFormatter<T>();
                }
            }
        }
    }
}
