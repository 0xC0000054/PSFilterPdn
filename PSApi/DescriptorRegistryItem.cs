/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2019 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace PSFilterLoad.PSApi
{
    [Serializable]
    public sealed class DescriptorRegistryItem
    {
        private readonly ReadOnlyDictionary<uint, AETEValue> values;
        private readonly bool isPersistent;

        /// <summary>
        /// Initializes a new instance of the <see cref="DescriptorRegistryItem"/> class.
        /// </summary>
        /// <param name="values">The values.</param>
        /// <param name="isPersistent"><c>true</c> if the item is persisted across host sessions; otherwise, <c>false</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="values"/> is null.</exception>
        public DescriptorRegistryItem(ReadOnlyDictionary<uint, AETEValue> values, bool isPersistent)
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
        public ReadOnlyDictionary<uint, AETEValue> Values
        {
            get
            {
                return values;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this <see cref="DescriptorRegistryItem"/> is persisted across host sessions.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this item is persisted across host sessions; otherwise, <c>false</c>.
        /// </value>
        public bool IsPersistent
        {
            get
            {
                return isPersistent;
            }
        }
    }
}
