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
using MessagePack.ImmutableCollection;
using MessagePack.Resolvers;

#nullable enable

namespace PSFilterPdn
{
    internal sealed class MessagePackResolver : IFormatterResolver
    {
        public static readonly IFormatterResolver Instance;
        public static readonly MessagePackSerializerOptions Options;

        private static readonly IFormatterResolver[] Resolvers;

        static MessagePackResolver()
        {
            Instance = new MessagePackResolver();
            Options = new MessagePackSerializerOptions(Instance).WithCompression(MessagePackCompression.Lz4BlockArray);
            Resolvers = new IFormatterResolver[]
            {
                BuiltinResolver.Instance,
                AttributeFormatterResolver.Instance,
                ImmutableCollectionResolver.Instance,
                DynamicGenericResolver.Instance
            };
        }

        private MessagePackResolver()
        {
        }

        public IMessagePackFormatter<T>? GetFormatter<T>() => FormatterCache<T>.Formatter;

        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T>? Formatter;

            static FormatterCache()
            {
                foreach (IFormatterResolver resolver in Resolvers)
                {
                    IMessagePackFormatter<T>? f = resolver.GetFormatter<T>();
                    if (f != null)
                    {
                        Formatter = f;
                        return;
                    }
                }
            }
        }
    }
}
