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
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace PSFilterLoad.PSApi
{
    [MessagePackObject]
    [MessagePackFormatter(typeof(Formatter))]
    internal sealed class DescriptorRegistryValues
    {
        private readonly Dictionary<string, Dictionary<uint, AETEValue>> persistedValues;
        private readonly Dictionary<string, Dictionary<uint, AETEValue>> sessionValues;

        public DescriptorRegistryValues()
        {
            persistedValues = new Dictionary<string, Dictionary<uint, AETEValue>>();
            sessionValues = new Dictionary<string, Dictionary<uint, AETEValue>>();
        }

        internal DescriptorRegistryValues(Dictionary<string, Dictionary<uint, AETEValue>> persistedValues)
        {
            this.persistedValues = persistedValues ?? throw new ArgumentNullException(nameof(persistedValues));
            sessionValues = new Dictionary<string, Dictionary<uint, AETEValue>>();
            Dirty = false;
        }

        private DescriptorRegistryValues(Dictionary<string, Dictionary<uint, AETEValue>> persistedValues,
                                        Dictionary<string, Dictionary<uint, AETEValue>> sessionValues)
        {
            this.persistedValues = persistedValues ?? throw new ArgumentNullException(nameof(persistedValues));
            this.sessionValues = sessionValues ?? throw new ArgumentNullException(nameof(sessionValues));
        }

        /// <summary>
        /// Gets or sets a value indicating whether the persisted settings have been marked as changed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the persisted settings have changed; otherwise, <c>false</c>.
        /// </value>
        public bool Dirty { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance has data.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has data; otherwise, <c>false</c>.
        /// </value>
        public bool HasData => persistedValues.Count > 0 || sessionValues.Count > 0;

        public void Add(string key, Dictionary<uint, AETEValue> values, bool isPersistent)
        {
            if (isPersistent)
            {
                persistedValues.AddOrUpdate(key, values);
                Dirty = true;
            }
            else
            {
                sessionValues.AddOrUpdate(key, values);
            }
        }

        public Dictionary<string, Dictionary<uint, AETEValue>> GetPersistedValuesReadOnly()
        {
            return persistedValues;
        }

        public void Remove(string key)
        {
            if (!persistedValues.Remove(key))
            {
                sessionValues.Remove(key);
            }
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out Dictionary<uint, AETEValue> value)
        {
            return persistedValues.TryGetValue(key, out value)
                || sessionValues.TryGetValue(key, out value);
        }

        private sealed class Formatter : IMessagePackFormatter<DescriptorRegistryValues>
        {
            public static readonly IMessagePackFormatter<DescriptorRegistryValues> Instance = new Formatter();

            private Formatter()
            {
            }

            public DescriptorRegistryValues Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                options.Security.DepthStep(ref reader);

                var formatter = options.Resolver.GetFormatterWithVerify<Dictionary<string, Dictionary<uint, AETEValue>>>();

                Dictionary<string, Dictionary<uint, AETEValue>> persistedValues = formatter.Deserialize(ref reader, options);
                Dictionary<string, Dictionary<uint, AETEValue>> sessionValues = formatter.Deserialize(ref reader, options);

                reader.Depth--;

                return new DescriptorRegistryValues(persistedValues, sessionValues);
            }

            public void Serialize(ref MessagePackWriter writer, DescriptorRegistryValues value, MessagePackSerializerOptions options)
            {
                var formatter = options.Resolver.GetFormatterWithVerify<Dictionary<string, Dictionary<uint, AETEValue>>>();

                formatter.Serialize(ref writer, value.persistedValues, options);
                formatter.Serialize(ref writer, value.sessionValues, options);
            }
        }
    }
}
