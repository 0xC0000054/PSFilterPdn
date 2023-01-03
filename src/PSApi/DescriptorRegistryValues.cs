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

namespace PSFilterLoad.PSApi
{
    [Serializable]
    internal sealed class DescriptorRegistryValues
    {
        private readonly Dictionary<string, DescriptorRegistryItem> persistedValues;
        private readonly Dictionary<string, DescriptorRegistryItem> sessionValues;
        private bool dirty;

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

            Dictionary<string, DescriptorRegistryItem> persistentItems = new(StringComparer.Ordinal);
            Dictionary<string, DescriptorRegistryItem> sessionItems = new(StringComparer.Ordinal);

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

            persistedValues = persistentItems;
            sessionValues = sessionItems;
            dirty = persistentValuesChanged;
        }

        internal DescriptorRegistryValues(Dictionary<string, DescriptorRegistryItem> persistedValues,
                                          bool dirty)
        {
            this.persistedValues = persistedValues ?? throw new ArgumentNullException(nameof(persistedValues));
            sessionValues = new Dictionary<string, DescriptorRegistryItem>();
            this.dirty = dirty;
        }

        public void AddToRegistry(Dictionary<string, DescriptorRegistryItem> registry)
        {
            if (persistedValues != null)
            {
                foreach (KeyValuePair<string, DescriptorRegistryItem> item in persistedValues)
                {
                    registry.Add(item.Key, item.Value);
                }
            }

            if (sessionValues != null)
            {
                foreach (KeyValuePair<string, DescriptorRegistryItem> item in sessionValues)
                {
                    registry.Add(item.Key, item.Value);
                }
            }
        }

        public Dictionary<string, DescriptorRegistryItem> GetPersistedValuesReadOnly()
        {
            return persistedValues;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the persisted settings have been marked as changed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the persisted settings have changed; otherwise, <c>false</c>.
        /// </value>
        public bool Dirty { get; set; }
    }
}
