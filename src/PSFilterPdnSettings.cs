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
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PSFilterPdn
{
    [DataContract(Name = "PSFilterPdnSettings", Namespace = "")]
    internal sealed class PSFilterPdnSettings
    {
        [DataMember(Name = "SearchDirectories")]
        private HashSet<string> searchDirectories;
        [DataMember(Name = "SearchSubdirectories")]
        private bool searchSubdirectories;

        /// <summary>
        /// Initializes a new instance of the <see cref="PSFilterPdnSettings"/> class.
        /// </summary>
        /// <param name="path">The path of the settings file.</param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
        public PSFilterPdnSettings()
        {
            Dirty = false;
            searchSubdirectories = true;
            searchDirectories = new HashSet<string>();
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has unsaved changes.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has unsaved changes; otherwise, <c>false</c>.
        /// </value>
        public bool Dirty { get; set; }

        /// <summary>
        /// Gets or sets the search directories.
        /// </summary>
        /// <value>
        /// The search directories.
        /// </value>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null</exception>
        public HashSet<string> SearchDirectories
        {
            get => searchDirectories;
            set
            {
                searchDirectories = value ?? throw new ArgumentNullException(nameof(value));
                Dirty = true;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether subdirectories are searched.
        /// </summary>
        /// <value>
        ///   <c>true</c> if subdirectories are searched; otherwise, <c>false</c>.
        /// </value>
        public bool SearchSubdirectories
        {
            get => searchSubdirectories;
            set
            {
                if (searchSubdirectories != value)
                {
                    searchSubdirectories = value;
                    Dirty = true;
                }
            }
        }
    }
}
