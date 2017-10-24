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

using System.Collections.ObjectModel;
using System.Runtime.Serialization;

namespace PSFilterLoad.PSApi
{
    /// <summary>
    /// The class that encapsulates an Adobe® Photoshop® filter plugin
    /// </summary>
    [DataContract()]
    public sealed class PluginData
    {
        private string fileName;
        private string entryPoint;
        private string category;
        private string title;
        private ReadOnlyCollection<FilterCaseInfo> filterInfo;
        private bool runWith32BitShim;
        private AETEData aete;
        private ReadOnlyCollection<string> moduleEntryPoints;

        [DataMember]
        public string FileName
        {
            get
            {
                return this.fileName;
            }
            private set // Required for DataContract serialization.
            {
                this.fileName = value;
            }
        }

        [DataMember]
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

        [DataMember]
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

        [DataMember]
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

        [DataMember]
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

        [DataMember]
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

        [DataMember]
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

        internal bool IsValid()
        {
            return (!string.IsNullOrEmpty(this.category) && !string.IsNullOrEmpty(this.title) && !string.IsNullOrEmpty(this.entryPoint));
        }
    }
}
