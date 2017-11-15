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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Forms;

namespace PSFilterPdn
{
    [Serializable]
    public sealed class FilterTreeNodes : IEnumerable<KeyValuePair<string, ReadOnlyCollection<TreeNode>>>
    {
        private readonly Dictionary<string, ReadOnlyCollection<TreeNode>> nodes;

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterTreeNodes"/> class.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <exception cref="ArgumentNullException"><paramref name="items"/> is null.</exception>
        internal FilterTreeNodes(IDictionary<string, List<TreeNode>> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException("items");
            }

            this.nodes = new Dictionary<string, ReadOnlyCollection<TreeNode>>(items.Count, StringComparer.Ordinal);

            foreach (var item in items)
            {
                this.nodes.Add(item.Key, new ReadOnlyCollection<TreeNode>(item.Value));
            }
        }

        public int Count
        {
            get
            {
                return this.nodes.Count;
            }
        }

        public ReadOnlyCollection<TreeNode> this[string key]
        {
            get
            {
                return this.nodes[key];
            }
        }

        public IEnumerator<KeyValuePair<string, ReadOnlyCollection<TreeNode>>> GetEnumerator()
        {
            return this.nodes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.nodes.GetEnumerator();
        }
    }
}
