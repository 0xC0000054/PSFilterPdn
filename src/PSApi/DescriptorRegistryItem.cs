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

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PSFilterLoad.PSApi
{
    [DataContract]
    [Serializable]
    internal sealed class DescriptorRegistryItem
    {
#pragma warning disable IDE0044 // Add readonly modifier
        [DataMember]
        private Dictionary<uint, AETEValue> values;
        [DataMember]
        private bool isPersistent;
#pragma warning restore IDE0044 // Add readonly modifier

        /// <summary>
        /// Initializes a new instance of the <see cref="DescriptorRegistryItem"/> class.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <param name="isPersistent"><c>true</c> if the item is persisted across host sessions; otherwise, <c>false</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="values"/> is null.</exception>
        public DescriptorRegistryItem(Dictionary<uint, AETEValue> values, bool isPersistent)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            this.values = values;
            this.isPersistent = isPersistent;
        }

        /// <summary>
        /// Gets the values.
        /// </summary>
        /// <value>
        /// The values.
        /// </value>
        public Dictionary<uint, AETEValue> Values => values;

        /// <summary>
        /// Gets a value indicating whether this <see cref="DescriptorRegistryItem"/> is persisted across host sessions.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this item is persisted across host sessions; otherwise, <c>false</c>.
        /// </value>
        public bool IsPersistent => isPersistent;
    }
}
