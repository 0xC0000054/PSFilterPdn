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

        public string FileName
        {
            get
            {
                return this.fileName;
            }
        }

        public string EntryPoint
        {
            get
            {
                return this.entryPoint;
            }
            internal set
            {
                this.entryPoint = value;
            }
        }

        public string Category
        {
            get
            {
                return this.category;
            }
            internal set
            {
                this.category = value;
            }
        }

        public string Title
        {
            get
            {
                return this.title;
            }
            internal set
            {
                this.title = value;
            }
        }

        public ReadOnlyCollection<FilterCaseInfo> FilterInfo
        {
            get
            {
                return this.filterInfo;
            }
            internal set
            {
                this.filterInfo = value;
            }
        }

        /// <summary>
        /// Used to run 32-bit plugins in 64-bit Paint.NET
        /// </summary>
        internal bool RunWith32BitShim
        {
            get
            {
                return this.runWith32BitShim;
            }
            set
            {
                this.runWith32BitShim = value;
            }
        }

        public AETEData Aete
        {
            get
            {
                return this.aete;
            }
            internal set
            {
                this.aete = value;
            }
        }

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

        internal PluginData(string fileName)
        {
            this.fileName = fileName;
            this.entryPoint = string.Empty;
            this.category = string.Empty;
            this.title = string.Empty;
            this.filterInfo = null;
            this.runWith32BitShim = false;
            this.aete = null;
            this.moduleEntryPoints = null;
        }

        internal PluginData(string fileName, string entryPoint, string category, string title)
        {
            this.fileName = fileName;
            this.entryPoint = entryPoint;
            this.category = category;
            this.title = title;
            this.filterInfo = null;
            this.runWith32BitShim = true;
            this.aete = null;
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
