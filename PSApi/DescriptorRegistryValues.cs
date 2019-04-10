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
using System.Collections.Generic;

namespace PSFilterLoad.PSApi
{
    [Serializable]
    public sealed class DescriptorRegistryValues
    {
        private readonly ReadOnlyDictionary<string, DescriptorRegistryItem> persistedValues;
        private readonly ReadOnlyDictionary<string, DescriptorRegistryItem> sessionValues;
        private bool dirty;

        /// <summary>
        /// Initializes a new instance of the <see cref="DescriptorRegistryValues"/> class.
        /// </summary>
        /// <param name="values">The registry values.</param>
        /// <exception cref="ArgumentNullException"><paramref name="values"/> is null.</exception>
        internal DescriptorRegistryValues(ReadOnlyDictionary<string, DescriptorRegistryItem> values) : this(values, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DescriptorRegistryValues"/> class.
        /// </summary>
        /// <param name="values">The registry values.</param>
        /// <param name="persistentValuesChanged"><c>true</c> if the persistent values have been changed; otherwise, <c>false</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="values"/> is null.</exception>
        public DescriptorRegistryValues(IDictionary<string, DescriptorRegistryItem> values, bool persistentValuesChanged)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            Dictionary<string, DescriptorRegistryItem> persistentItems = new Dictionary<string, DescriptorRegistryItem>(StringComparer.Ordinal);
            Dictionary<string, DescriptorRegistryItem> sessionItems = new Dictionary<string, DescriptorRegistryItem>(StringComparer.Ordinal);

            foreach (KeyValuePair<string, DescriptorRegistryItem> item in values)
            {
                if (item.Value.IsPersistent)
                {
                    persistentItems.Add(item.Key, item.Value);
                }
                else
                {
                    sessionItems.Add(item.Key, item.Value);
                }
            }

            persistedValues = new ReadOnlyDictionary<string, DescriptorRegistryItem>(persistentItems);
            sessionValues = new ReadOnlyDictionary<string, DescriptorRegistryItem>(sessionItems);
            dirty = persistentValuesChanged;
        }

        /// <summary>
        /// Gets the values that are persisted between host sessions.
        /// </summary>
        /// <value>
        /// The values that are persisted between host sessions.
        /// </value>
        public ReadOnlyDictionary<string, DescriptorRegistryItem> PersistedValues => persistedValues;

        /// <summary>
        /// Gets the values that are stored for the current session.
        /// </summary>
        /// <value>
        /// The values that are stored for the current session.
        /// </value>
        public ReadOnlyDictionary<string, DescriptorRegistryItem> SessionValues => sessionValues;

        /// <summary>
        /// Gets or sets a value indicating whether the persisted settings have been marked as changed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the persisted settings have changed; otherwise, <c>false</c>.
        /// </value>
        public bool Dirty
        {
            get => dirty;
            set => dirty = value;
        }
    }
}
