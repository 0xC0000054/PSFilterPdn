/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2019 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterPdn.EnableInfo;
using System;
using System.Collections.Generic;
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
        [NonSerialized]
        private readonly string enableInfo;
        [NonSerialized]
        private Expression enableInfoExpression;
        [NonSerialized]
        private bool enableInfoExpressionParsed;

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
                return fileName;
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
                return entryPoint;
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
                return category;
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
                return title;
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
                return filterInfo;
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
                return runWith32BitShim;
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
                return aete;
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
                return moduleEntryPoints;
            }
            internal set
            {
                moduleEntryPoints = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginData"/> class.
        /// </summary>
        /// <param name="fileName">The file path of the filter.</param>
        /// <param name="entryPoint">The filter entry point.</param>
        /// <param name="category">The filter category.</param>
        /// <param name="title">The filter title.</param>
        internal PluginData(string fileName, string entryPoint, string category, string title) : this(fileName, entryPoint, category, title,
            null, true, null, null)
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
            bool runWith32BitShim, AETEData aete, string enableInfo)
        {
            this.fileName = fileName;
            this.entryPoint = entryPoint;
            this.category = category;
            this.title = title;
            this.filterInfo = filterInfo;
            this.runWith32BitShim = runWith32BitShim;
            this.aete = aete;
            this.enableInfo = enableInfo;
            moduleEntryPoints = null;
            enableInfoExpression = null;
            enableInfoExpressionParsed = false;
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

            return (string.Equals(fileName, other.fileName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(entryPoint, other.entryPoint, StringComparison.Ordinal) &&
                    string.Equals(category, other.category, StringComparison.Ordinal) &&
                    string.Equals(title, other.title, StringComparison.Ordinal));
        }

        public FilterCase GetFilterTransparencyMode(ImageModes imageMode, bool hasSelection, Func<bool> hasTransparency)
        {
            if (hasTransparency == null)
            {
                throw new ArgumentNullException(nameof(hasTransparency));
            }

            FilterCase filterCase;

            // Some filters do not handle transparency correctly despite what their filterInfo says.
            if (filterInfo == null ||
                category.Equals("Axion", StringComparison.Ordinal) ||
                category.Equals("Vizros 4", StringComparison.Ordinal) && title.StartsWith("Lake", StringComparison.Ordinal) ||
                category.Equals("Nik Collection", StringComparison.Ordinal) && title.StartsWith("Dfine 2", StringComparison.Ordinal))
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

                if (!filterInfo[filterCaseIndex].IsSupported())
                {
                    if (hasTransparency())
                    {
                        if (filterInfo[filterCaseIndex + 2].IsSupported())
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
                            if (filterInfo[(int)FilterCase.FloatingSelection - 1].IsSupported())
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
                hash = (hash * 127) + (fileName != null ? StringComparer.OrdinalIgnoreCase.GetHashCode(fileName) : 0);
                hash = (hash * 127) + (entryPoint != null ? entryPoint.GetHashCode() : 0);
                hash = (hash * 127) + (category != null ? category.GetHashCode() : 0);
                hash = (hash * 127) + (title != null ? title.GetHashCode() : 0);
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
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="hostState"/> is null.
        /// </exception>
        internal bool SupportsHostState(int imageWidth, int imageHeight, bool hasTransparency, HostState hostState)
        {
            if (hostState == null)
            {
                throw new ArgumentNullException(nameof(hostState));
            }

            bool result = true;

            const ImageModes imageMode = ImageModes.RGB;

            FilterCase filterCase = GetFilterTransparencyMode(imageMode, hostState.HasSelection, () => hasTransparency);

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

                EnableInfoVariables variables = new EnableInfoVariables(imageWidth, imageHeight, imageMode, hasTransparencyMask,
                                                                        targetChannelCount, trueChannelCount, hostState);
                try
                {
                    if (!enableInfoExpressionParsed)
                    {
                        enableInfoExpressionParsed = true;

                        enableInfoExpression = EnableInfoParser.Parse(enableInfo);
                    }

                    if (enableInfoExpression != null)
                    {
                        result = new EnableInfoInterpreter(variables).Evaluate(enableInfoExpression);
                    }
                }
                catch (EnableInfoException)
                {
                    // Ignore any errors that occur when parsing or evaluating the enable info expression.
                }
            }

            if (filterInfo != null)
            {
                result &= filterInfo[(int)filterCase - 1].IsSupported();
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

        internal bool IsValid()
        {
            return (!string.IsNullOrEmpty(category) && !string.IsNullOrEmpty(title) && !string.IsNullOrEmpty(entryPoint));
        }
    }
}
