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

using MessagePack;
using PSFilterPdn.EnableInfo;
using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    /// <summary>
    /// The class that encapsulates an Adobe® Photoshop® filter plugin
    /// </summary>
    [MessagePackObject]
    internal sealed class PluginData : IEquatable<PluginData>
    {
        private readonly string enableInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginData"/> class.
        /// </summary>
        /// <param name="fileName">The file path of the filter.</param>
        /// <param name="entryPoint">The filter entry point.</param>
        /// <param name="category">The filter category.</param>
        /// <param name="title">The filter title.</param>
        /// <param name="processorArchitecture">The processor architecture of the plug-in.</param>
        internal PluginData(string fileName,
                            string entryPoint,
                            string category,
                            string title,
                            Architecture processorArchitecture) :
            this(fileName, entryPoint, category, title, null, true, null, null, processorArchitecture)
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
        /// <param name="processorArchitecture">The processor architecture of the plug-in.</param>
        internal PluginData(string fileName,
                            string entryPoint,
                            string category,
                            string title,
                            FilterCaseInfoCollection filterInfo,
                            bool runWith32BitShim,
                            AETEData aete,
                            string enableInfo,
                            Architecture processorArchitecture)
        {
            this.enableInfo = enableInfo;
            FileName = fileName;
            EntryPoint = entryPoint;
            Category = category;
            Title = title;
            FilterInfo = filterInfo;
            RunWith32BitShim = runWith32BitShim;
            Aete = aete;
            ModuleEntryPoints = null;
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
        /// Gets a value indicating whether the filter should be run with the 32-bit surrogate process.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the filter should be run with the 32-bit surrogate process; otherwise, <c>false</c>.
        /// </value>
        internal bool RunWith32BitShim { get; }

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

        /// <summary>
        /// Gets the mode that indicates how the filter processes transparency.
        /// </summary>
        /// <param name="hasSelection"><c>true</c> if the host has an active selection; otherwise, <c>false</c>.</param>
        /// <param name="hasTransparency">A delegate that allows the method to determine if the image has transparency.</param>
        /// <returns>One of the <see cref="FilterCase"/> values indicating how the filter processes transparency.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="hasTransparency"/> is null.</exception>
        public FilterCase GetFilterTransparencyMode(bool hasSelection, Func<bool> hasTransparency)
        {
            if (hasTransparency == null)
            {
                throw new ArgumentNullException(nameof(hasTransparency));
            }

            FilterCase filterCase;

            // Some filters do not handle transparency correctly despite what their filterInfo says.
            if (FilterInfo == null ||
                Category.Equals("Axion", StringComparison.Ordinal) ||
                Category.Equals("Vizros 4", StringComparison.Ordinal) && Title.StartsWith("Lake", StringComparison.Ordinal) ||
                Category.Equals("Nik Collection", StringComparison.Ordinal) && Title.StartsWith("Dfine 2", StringComparison.Ordinal))
            {
                if (hasTransparency())
                {
                    filterCase = FilterCase.FloatingSelection;
                }
                else
                {
                    filterCase = hasSelection ? FilterCase.FlatImageWithSelection : FilterCase.FlatImageNoSelection;
                }
            }
            else
            {
                filterCase = hasSelection ? FilterCase.EditableTransparencyWithSelection : FilterCase.EditableTransparencyNoSelection;

                int filterCaseIndex = (int)filterCase - 1;

                if (!FilterInfo[filterCaseIndex].IsSupported)
                {
                    if (hasTransparency())
                    {
                        if (FilterInfo[filterCaseIndex + 2].IsSupported)
                        {
                            switch (filterCase)
                            {
                                case FilterCase.EditableTransparencyNoSelection:
                                    filterCase = FilterCase.ProtectedTransparencyNoSelection;
                                    break;
                                case FilterCase.EditableTransparencyWithSelection:
                                    filterCase = FilterCase.ProtectedTransparencyWithSelection;
                                    break;
                            }
                        }
                        else
                        {
                            // If the protected transparency modes are not supported use the next most appropriate mode.
                            if (FilterInfo[FilterCase.FloatingSelection].IsSupported)
                            {
                                filterCase = FilterCase.FloatingSelection;
                            }
                            else
                            {
                                switch (filterCase)
                                {
                                    case FilterCase.EditableTransparencyNoSelection:
                                        filterCase = FilterCase.FlatImageNoSelection;
                                        break;
                                    case FilterCase.EditableTransparencyWithSelection:
                                        filterCase = FilterCase.FlatImageWithSelection;
                                        break;
                                }
                            }
                        }
                    }
                    else
                    {
                        switch (filterCase)
                        {
                            case FilterCase.EditableTransparencyNoSelection:
                                filterCase = FilterCase.FlatImageNoSelection;
                                break;
                            case FilterCase.EditableTransparencyWithSelection:
                                filterCase = FilterCase.FlatImageWithSelection;
                                break;
                        }
                    }
                }
            }

            return filterCase;
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

        /// <summary>
        /// Determines whether the filter can process the specified image and host application state.
        /// </summary>
        /// <param name="imageWidth">The width of the image.</param>
        /// <param name="imageHeight">The height of the image.</param>
        /// <param name="hasTransparency">Indicates if the image has transparency.</param>
        /// <param name="hostState">The current state of the host application.</param>
        /// <returns>
        /// <c>true</c> if the filter can process the image and host application state; otherwise, <c>false</c>.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="hostState"/> is null.
        /// </exception>
        internal bool SupportsHostState(int imageWidth, int imageHeight, Func<bool> hasTransparency, HostState hostState)
        {
            if (hostState == null)
            {
                throw new ArgumentNullException(nameof(hostState));
            }

            bool result = true;

            const ImageMode imageMode = ImageMode.RGB;

            FilterCase filterCase = GetFilterTransparencyMode(hostState.HasSelection, hasTransparency);

            if (!string.IsNullOrEmpty(enableInfo))
            {
                int targetChannelCount = 3;
                int trueChannelCount;
                bool hasTransparencyMask;

                switch (filterCase)
                {
                    case FilterCase.EditableTransparencyNoSelection:
                    case FilterCase.EditableTransparencyWithSelection:
                    case FilterCase.ProtectedTransparencyNoSelection:
                    case FilterCase.ProtectedTransparencyWithSelection:
                        trueChannelCount = 4;
                        hasTransparencyMask = true;
                        break;
                    case FilterCase.FlatImageNoSelection:
                    case FilterCase.FlatImageWithSelection:
                    case FilterCase.FloatingSelection:
                    default:
                        trueChannelCount = 3;
                        hasTransparencyMask = false;
                        break;
                }

                EnableInfoVariables variables = new(imageWidth,
                                                    imageHeight,
                                                    imageMode,
                                                    hasTransparencyMask,
                                                    targetChannelCount,
                                                    trueChannelCount,
                                                    hostState);

                bool? enableInfoResult = EnableInfoResultCache.Instance.TryGetValue(enableInfo, variables);
                if (enableInfoResult.HasValue)
                {
                    result = enableInfoResult.Value;
                }
            }

            if (FilterInfo != null)
            {
                result &= FilterInfo[filterCase].IsSupported;
            }

            return result;
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
