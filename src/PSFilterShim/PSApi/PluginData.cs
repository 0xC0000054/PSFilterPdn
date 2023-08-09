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
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    /// <summary>
    /// The class that encapsulates an Adobe® Photoshop® filter plugin
    /// </summary>
    internal sealed class PluginData : IEquatable<PluginData>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginData"/> class.
        /// </summary>
        /// <param name="fileName">The file path of the filter.</param>
        /// <param name="entryPoint">The filter entry point.</param>
        /// <param name="category">The filter category.</param>
        /// <param name="title">The filter title.</param>
        /// <param name="filterInfo">The filter information used to determine how transparency is processed..</param>
        /// <param name="aete">The AETE scripting information.</param>
        /// <param name="processorArchitecture">The processor architecture of the plug-in.</param>
        internal PluginData(string fileName,
                            string entryPoint,
                            string category,
                            string title,
                            FilterCaseInfoCollection filterInfo,
                            AETEData aete,
                            ReadOnlyCollection<string> moduleEntryPoints,
                            Architecture processorArchitecture)
        {
            FileName = fileName;
            EntryPoint = entryPoint;
            Category = category;
            Title = title;
            FilterInfo = filterInfo;
            Aete = aete;
            ModuleEntryPoints = moduleEntryPoints;
            ProcessorArchitecture = processorArchitecture;
        }

        /// <summary>
        /// Gets the file path of the filter.
        /// </summary>
        /// <value>
        /// The file path of the filter.
        /// </value>
        public string FileName { get; }

        /// <summary>
        /// Gets the filter entry point.
        /// </summary>
        /// <value>
        /// The entry point.
        /// </value>
        public string EntryPoint { get; }

        /// <summary>
        /// Gets the filter category.
        /// </summary>
        /// <value>
        /// The filter category.
        /// </value>
        public string Category { get; }

        /// <summary>
        /// Gets the filter title.
        /// </summary>
        /// <value>
        /// The filter title.
        /// </value>
        public string Title { get; }

        /// <summary>
        /// Gets the filter information used to determine how transparency is processed.
        /// </summary>
        /// <value>
        /// The filter information used to determine how transparency is processed.
        /// </value>
        public FilterCaseInfoCollection FilterInfo { get; }

        /// <summary>
        /// Gets the AETE scripting information.
        /// </summary>
        /// <value>
        /// The AETE scripting information.
        /// </value>
        public AETEData Aete { get; }

        /// <summary>
        /// Gets a collection containing all of the entry points in the filter module.
        /// </summary>
        /// <value>
        /// The collection containing all of the entry points in the module.
        /// </value>
        public ReadOnlyCollection<string> ModuleEntryPoints { get; internal set; }

        /// <summary>
        /// Gets the processor architecture that the plug-in was built for.
        /// </summary>
        /// <value>
        /// The processor architecture.
        /// </value>
        public Architecture ProcessorArchitecture { get; }

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

            return string.Equals(FileName, other.FileName, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(EntryPoint, other.EntryPoint, StringComparison.Ordinal) &&
                   string.Equals(Category, other.Category, StringComparison.Ordinal) &&
                   string.Equals(Title, other.Title, StringComparison.Ordinal);
        }

        public override int GetHashCode()
        {
            int hash = 23;

            unchecked
            {
                hash = (hash * 127) + (FileName != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(FileName) : 0);
                hash = (hash * 127) + (EntryPoint != null ? EntryPoint.GetHashCode() : 0);
                hash = (hash * 127) + (Category != null ? Category.GetHashCode() : 0);
                hash = (hash * 127) + (Title != null ? Title.GetHashCode() : 0);
            }

            return hash;
        }

        public static bool operator ==(PluginData pluginData1, PluginData pluginData2)
        {
            if (ReferenceEquals(pluginData1, pluginData2))
            {
                return true;
            }

            if (((object)pluginData1) == null || ((object)pluginData2) == null)
            {
                return false;
            }

            return pluginData1.Equals(pluginData2);
        }

        public static bool operator !=(PluginData pluginData1, PluginData pluginData2)
        {
            return !(pluginData1 == pluginData2);
        }
    }
}
