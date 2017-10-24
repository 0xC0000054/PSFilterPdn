/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2017 Nicholas Hayes
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
        private bool changed;

        /// <summary>
        /// Initializes a new instance of the <see cref="DescriptorRegistryValues"/> class.
        /// </summary>
        /// <param name="values">The registry values.</param>
        /// <exception cref="ArgumentNullException"><paramref name="values"/> is null.</exception>
        public DescriptorRegistryValues(IDictionary<string, DescriptorRegistryItem> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException("values");
            }

            Dictionary<string, DescriptorRegistryItem> persistentItems = new Dictionary<string, DescriptorRegistryItem>(StringComparer.Ordinal);
            Dictionary<string, DescriptorRegistryItem> sessionItems = new Dictionary<string, DescriptorRegistryItem>(StringComparer.Ordinal);

            foreach (var item in values)
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

            this.persistedValues = new ReadOnlyDictionary<string, DescriptorRegistryItem>(persistentItems);
            this.sessionValues = new ReadOnlyDictionary<string, DescriptorRegistryItem>(sessionItems);
            this.changed = true;
        }

        /// <summary>
        /// Gets the values that are persisted between host sessions.
        /// </summary>
        /// <value>
        /// The values that are persisted between host sessions.
        /// </value>
        public ReadOnlyDictionary<string, DescriptorRegistryItem> PersistedValues
        {
            get
            {
                return this.persistedValues;
            }
        }

        /// <summary>
        /// Gets the values that are stored for the current session.
        /// </summary>
        /// <value>
        /// The values that are stored for the current session.
        /// </value>
        public ReadOnlyDictionary<string, DescriptorRegistryItem> SessionValues
        {
            get
            {
                return this.sessionValues;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has changed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance changed; otherwise, <c>false</c>.
        /// </value>
        public bool Changed
        {
            get
            {
                return this.changed;
            }
            set
            {
                this.changed = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has persisted data.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has persisted data; otherwise, <c>false</c>.
        /// </value>
        internal bool HasPersistedData
        {
            get
            {
                return (this.persistedValues.Count > 0);
            }
        }
    }
}
