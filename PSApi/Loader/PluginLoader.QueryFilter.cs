/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2022 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;

namespace PSFilterLoad.PSApi
{
    internal static partial class PluginLoader
    {
        private sealed class QueryFilter
        {
            public readonly string fileName;
            public readonly uint platformEntryPoint;
            public readonly bool runWith32BitShim;
            public List<PluginData> plugins;

            /// <summary>
            /// Initializes a new instance of the <see cref="QueryFilter"/> class.
            /// </summary>
            /// <param name="fileName">The file name of the plug-in.</param>
            /// <param name="platform">The processor architecture that the plug-in was built for.</param>
            /// <exception cref="System.PlatformNotSupportedException">The processor architecture specified by <paramref name="platform"/> is not supported.</exception>
            public QueryFilter(string fileName, ProcessorArchitecture platform)
            {
                this.fileName = fileName;
                switch (platform)
                {
                    case ProcessorArchitecture.X86:
                        platformEntryPoint = PIPropertyID.PIWin32X86CodeProperty;
                        break;
                    case ProcessorArchitecture.X64:
                        platformEntryPoint = PIPropertyID.PIWin64X86CodeProperty;
                        break;
                    case ProcessorArchitecture.Arm64:
                        platformEntryPoint = PIPropertyID.PIWin64ARMCodeProperty;
                        break;
                    case ProcessorArchitecture.Unknown:
                    default:
                        throw new PlatformNotSupportedException($"No platform entry point was defined for { nameof(ProcessorArchitecture) }.{ platform }.");
                }
                plugins = new List<PluginData>();
                runWith32BitShim = platform == ProcessorArchitecture.X86 && ProcessInformation.Architecture != ProcessorArchitecture.X86;
            }
        }
    }
}
