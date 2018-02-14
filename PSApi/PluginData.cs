/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2018 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace PSFilterLoad.PSApi
{
    /// <summary>
    /// The class that encapsulates an Adobe® Photoshop® filter plugin
    /// </summary>
    [DataContract()]
    public sealed class PluginData : IEquatable<PluginData>
    {
        [DataMember(Name = nameof(FileName))]
        private string fileName;
        [DataMember(Name = nameof(EntryPoint))]
        private string entryPoint;
        [DataMember(Name = nameof(Category))]
        private string category;
        [DataMember(Name = nameof(Title))]
        private string title;
        [DataMember(Name = nameof(FilterInfo))]
        private ReadOnlyCollection<FilterCaseInfo> filterInfo;
        private bool runWith32BitShim;
        [DataMember(Name = nameof(Aete))]
        private AETEData aete;
        [DataMember(Name = nameof(ModuleEntryPoints))]
        private ReadOnlyCollection<string> moduleEntryPoints;

        /// <summary>
        /// Gets the file path of the filter.
        /// </summary>
        /// <value>
        /// The file path of the filter.
        /// </value>
        public string FileName
        {
            get
            {
                return this.fileName;
            }
        }

        /// <summary>
        /// Gets the filter entry point.
        /// </summary>
        /// <value>
        /// The entry point.
        /// </value>
        public string EntryPoint
        {
            get
            {
                return this.entryPoint;
            }
        }

        /// <summary>
        /// Gets the filter category.
        /// </summary>
        /// <value>
        /// The filter category.
        /// </value>
        public string Category
        {
            get
            {
                return this.category;
            }
        }

        /// <summary>
        /// Gets the filter title.
        /// </summary>
        /// <value>
        /// The filter title.
        /// </value>
        public string Title
        {
            get
            {
                return this.title;
            }
        }

        /// <summary>
        /// Gets the filter information used to determine how transparency is processed.
        /// </summary>
        /// <value>
        /// The filter information used to determine how transparency is processed.
        /// </value>
        public ReadOnlyCollection<FilterCaseInfo> FilterInfo
        {
            get
            {
                return this.filterInfo;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the filter should be run with the 32-bit surrogate process.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the filter should be run with the 32-bit surrogate process; otherwise, <c>false</c>.
        /// </value>
        internal bool RunWith32BitShim
        {
            get
            {
                return this.runWith32BitShim;
            }
        }

        /// <summary>
        /// Gets the AETE scripting information.
        /// </summary>
        /// <value>
        /// The AETE scripting information.
        /// </value>
        public AETEData Aete
        {
            get
            {
                return this.aete;
            }
        }

        /// <summary>
        /// Gets a collection containing all of the entry points in the filter module.
        /// </summary>
        /// <value>
        /// The collection containing all of the entry points in the module.
        /// </value>
        public ReadOnlyCollection<string> ModuleEntryPoints
        {
            get
            {
                return this.moduleEntryPoints;
            }
            internal set
            {
                this.moduleEntryPoints = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginData"/> class.
        /// </summary>
        /// <param name="fileName">The file path of the filter.</param>
        /// <param name="entryPoint">The filter entry point.</param>
        /// <param name="category">The filter category.</param>
        /// <param name="title">The filter title.</param>
        internal PluginData(string fileName, string entryPoint, string category, string title) : this(fileName, entryPoint, category, title, null, true, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginData" /> class.
        /// </summary>
        /// <param name="fileName">The file path of the filter.</param>
        /// <param name="entryPoint">The filter entry point.</param>
        /// <param name="category">The filter category.</param>
        /// <param name="title">The filter title.</param>
        /// <param name="filterInfo">The filter information used to determine how transparency is processed..</param>
        /// <param name="runWith32BitShim"><c>true</c> if the filter should be run with the 32-bit surrogate process; otherwise, <c>false</c>.</param>
        /// <param name="aete">The AETE scripting information.</param>
        internal PluginData(string fileName, string entryPoint, string category, string title, ReadOnlyCollection<FilterCaseInfo> filterInfo,
            bool runWith32BitShim, AETEData aete)
        {
            this.fileName = fileName;
            this.entryPoint = entryPoint;
            this.category = category;
            this.title = title;
            this.filterInfo = filterInfo;
            this.runWith32BitShim = runWith32BitShim;
            this.aete = aete;
            this.moduleEntryPoints = null;
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            return Equals(obj as PluginData);
        }

        public bool Equals(PluginData other)
        {
            if (other == null)
            {
                return false;
            }

            return (string.Equals(this.fileName, other.fileName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(this.entryPoint, other.entryPoint, StringComparison.Ordinal) &&
                    string.Equals(this.category, other.category, StringComparison.Ordinal) &&
                    string.Equals(this.title, other.title, StringComparison.Ordinal));
        }

        public override int GetHashCode()
        {
            int hash = 23;

            unchecked
            {
                hash = (hash * 127) + (this.fileName != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(this.fileName) : 0);
                hash = (hash * 127) + (this.entryPoint != null ? this.entryPoint.GetHashCode() : 0);
                hash = (hash * 127) + (this.category != null ? this.category.GetHashCode() : 0);
                hash = (hash * 127) + (this.title != null ? this.title.GetHashCode() : 0);
            }

            return hash;
        }

        public static bool operator ==(PluginData p1, PluginData p2)
        {
            if (ReferenceEquals(p1, p2))
            {
                return true;
            }

            if (((object)p1) == null || ((object)p2) == null)
            {
                return false;
            }

            return p1.Equals(p2);
        }

        public static bool operator !=(PluginData p1, PluginData p2)
        {
            return !(p1 == p2);
        }

        internal bool IsValid()
        {
            return (!string.IsNullOrEmpty(this.category) && !string.IsNullOrEmpty(this.title) && !string.IsNullOrEmpty(this.entryPoint));
        }
    }
}
