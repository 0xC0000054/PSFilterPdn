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
using System.Collections.Generic;

namespace PSFilterLoad.PSApi
{
    /// <summary>
    /// Represents the parameters used to reapply a filter with the same settings.
    /// </summary>
    [MessagePackObject]
    [MessagePackFormatter(typeof(Formatter))]
    internal sealed partial class ParameterData
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterData"/> class.
        /// </summary>
        /// <param name="globals">The globals.</param>
        /// <param name="aete">The dictionary containing the scripting parameters.</param>
        internal ParameterData(GlobalParameters globals, Dictionary<uint, AETEValue>? aete)
        {
            GlobalParameters = globals;

            if ((aete is not null) && aete.Count > 0)
            {
                ScriptingData = aete;
            }
            else
            {
                ScriptingData = null;
            }
        }

        /// <summary>
        /// Gets the filter's global parameters.
        /// </summary>
        internal GlobalParameters GlobalParameters { get; }

        /// <summary>
        /// Gets the filter's AETE scripting values.
        /// </summary>
        internal Dictionary<uint, AETEValue>? ScriptingData { get; }

        /// <summary>
        /// Gets a value indicating whether this instance should be serialized.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> if this instance should be serialized; otherwise, <see langword="false"/>.
        /// </returns>
        internal bool ShouldSerialize()
        {
            // Ignore the filters that only use the data handle, e.g. Filter Factory.
            //
            // Filter Factory-based plugins appear to store compiled code in the data handle that is
            // specific to the address space layout of the process when the filter was first invoked.
            //
            // Because PSFilterPdn starts a new instance of the PSFilterShim process for each filter it
            // executes, this behavior would cause the process to crash with an access violation when running
            // a Filter Factory-based plugin with its last used parameters.

            return GlobalParameters.GetParameterDataBytes() != null || ScriptingData != null;
        }
    }
}
