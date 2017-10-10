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

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigDialogState"/> class.
        /// </summary>
        /// <param name="expandedNodes">The expanded nodes.</param>
        public ConfigDialogState(ReadOnlyCollection<string> expandedNodes)
        {
            this.expandedNodes = expandedNodes;
        }

        /// <summary>
        /// Gets the expanded nodes.
        /// </summary>
        /// <value>
        /// The expanded nodes.
        /// </value>
        public ReadOnlyCollection<string> ExpandedNodes
        {
            get
            {
                return this.expandedNodes;
            }
        }
    }
}
