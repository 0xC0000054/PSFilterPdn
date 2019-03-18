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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Forms;

namespace PSFilterPdn
{
    [Serializable]
    public sealed class FilterTreeNodeCollection : IEnumerable<KeyValuePair<string, ReadOnlyCollection<TreeNodeEx>>>
    {
        private readonly Dictionary<string, ReadOnlyCollection<TreeNodeEx>> nodes;

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterTreeNodeCollection"/> class.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is null.</exception>
        internal FilterTreeNodeCollection(IDictionary<string, List<TreeNodeEx>> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            nodes = new Dictionary<string, ReadOnlyCollection<TreeNodeEx>>(items.Count, StringComparer.Ordinal);

            foreach (var item in items)
            {
                nodes.Add(item.Key, new ReadOnlyCollection<TreeNodeEx>(item.Value));
            }
        }

        public int Count
        {
            get
            {
                return nodes.Count;
            }
        }

        public ReadOnlyCollection<TreeNodeEx> this[string key]
        {
            get
            {
                return nodes[key];
            }
        }

        public IEnumerator<KeyValuePair<string, ReadOnlyCollection<TreeNodeEx>>> GetEnumerator()
        {
            return nodes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return nodes.GetEnumerator();
        }
    }
}
