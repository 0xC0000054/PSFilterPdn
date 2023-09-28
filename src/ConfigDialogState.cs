/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2023 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.ObjectModel;

namespace PSFilterPdn
{
    /// <summary>
    /// Represents configuration dialog data that is persisted in the effect token
    /// </summary>
    [Serializable]
    public sealed class ConfigDialogState
    {
        private readonly ReadOnlyCollection<string> expandedNodes;
        private readonly FilterTreeNodeCollection filterTreeNodes;
        private readonly ReadOnlyCollection<string> searchDirectories;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigDialogState"/> class.
        /// </summary>
        /// <param name="expandedNodes">The expanded nodes.</param>
        /// <param name="nodes">The filter tree nodes.</param>
        /// <param name="directories">The search directories.</param>
        public ConfigDialogState(ReadOnlyCollection<string> expandedNodes, FilterTreeNodeCollection nodes, ReadOnlyCollection<string> directories)
        {
            this.expandedNodes = expandedNodes;
            filterTreeNodes = nodes;
            searchDirectories = directories;
        }

        /// <summary>
        /// Gets the expanded nodes.
        /// </summary>
        /// <value>
        /// The expanded nodes.
        /// </value>
        public ReadOnlyCollection<string> ExpandedNodes => expandedNodes;

        /// <summary>
        /// Gets the filter tree nodes.
        /// </summary>
        /// <value>
        /// The filter tree nodes.
        /// </value>
        public FilterTreeNodeCollection FilterTreeNodes => filterTreeNodes;

        /// <summary>
        /// Gets the search directories.
        /// </summary>
        /// <value>
        /// The search directories.
        /// </value>
        public ReadOnlyCollection<string> SearchDirectories => searchDirectories;
    }
}
