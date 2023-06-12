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

        /// <summary>
        /// Gets or sets a value indicating whether the persisted settings have been marked as changed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the persisted settings have changed; otherwise, <c>false</c>.
        /// </value>
        public bool Dirty { get; set; }

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

        public bool TryGetValue(string key, out Dictionary<uint, AETEValue> value)
        {
            return persistedValues.TryGetValue(key, out value)
                || sessionValues.TryGetValue(key, out value);
        }
    }
}
