/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2016 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace PSFilterLoad.PSApi
{
    /// <summary>
    /// The class that encapsulates an Adobe® Photoshop® filter plugin
    /// </summary>
    [DataContract()]
    internal sealed class PluginData
    {
        [DataMember]
        public string fileName;
        [DataMember]
        public string entryPoint;
        [DataMember]
        public string category;
        [DataMember]
        public string title;
        [DataMember]
        public FilterCaseInfo[] filterInfo;
        /// <summary>
        /// Used to run 32-bit plugins in 64-bit Paint.NET
        /// </summary>
        public bool runWith32BitShim;
        [DataMember]
        public AETEData aete;
        [DataMember]
        public string[] moduleEntryPoints;

        public PluginData(string fileName)
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

        public PluginData(string fileName, string entryPoint, string category, string title, FilterCaseInfo[] info, AETEData aete)
        {
            this.fileName = fileName;
            this.entryPoint = entryPoint;
            this.category = category;
            this.title = title;
            this.filterInfo = info;
            this.runWith32BitShim = false;
            this.aete = aete;
            this.moduleEntryPoints = null;
        }

        public bool IsValid()
        {
            return (!string.IsNullOrEmpty(this.category) && !string.IsNullOrEmpty(this.title) && !string.IsNullOrEmpty(this.entryPoint));
        }
    }
}
