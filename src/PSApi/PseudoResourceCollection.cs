/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2025 Nicholas Hayes
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
    /// <summary>
    /// Represents a collection of Pseudo-Resources
    /// </summary>
    /// <seealso cref="Collection{PSResource}" />
    [MessagePackObject]
    [MessagePackFormatter(typeof(Formatter))]
    internal sealed class PseudoResourceCollection : Collection<PseudoResource>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PseudoResourceCollection"/> class.
        /// </summary>
        public PseudoResourceCollection() : base()
        {
        }

        private PseudoResourceCollection(IList<PseudoResource> items) : base(items)
        {
        }

        /// <summary>
        /// Finds the pseudo-resource with the specified type and index.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="index">The index.</param>
        /// <returns>The pseudo-resource with the specified type and index, if found; otherwise, null.</returns>
        public PseudoResource? Find(uint type, int index)
        {
            IList<PseudoResource> items = Items;

            for (int i = 0; i < items.Count; i++)
            {
                PseudoResource item = items[i];
                if (item.Equals(type, index))
                {
                    return item;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the zero-based index of the pseudo-resource with the specified type and index.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="index">The index.</param>
        /// <returns>The zero-based index of pseudo-resource with the specified type and index, if found; otherwise, -1.</returns>
        public int FindIndex(uint type, int index)
        {
            IList<PseudoResource> items = Items;

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Equals(type, index))
                {
                    return i;
                }
            }

            return -1;
        }

        private sealed class Formatter : IMessagePackFormatter<PseudoResourceCollection>
        {
            public PseudoResourceCollection Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options)
            {
                options.Security.DepthStep(ref reader);

                IList<PseudoResource> items = options.Resolver.GetFormatterWithVerify<IList<PseudoResource>>().Deserialize(ref reader, options);

                reader.Depth--;

                return new PseudoResourceCollection(items);
            }

            public void Serialize(ref MessagePackWriter writer, PseudoResourceCollection value, MessagePackSerializerOptions options)
            {
                options.Resolver.GetFormatterWithVerify<IList<PseudoResource>>().Serialize(ref writer, value.Items, options);
            }
        }
    }
}
