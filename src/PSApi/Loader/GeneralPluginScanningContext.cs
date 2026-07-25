/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2026 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterLoad.PSApi.Diagnostics;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi.Loader
{
    internal sealed class GeneralPluginScanningContext
    {
        public readonly string fileName;
        public readonly IPluginLoadingLogger logger;
        public readonly Architecture processorArchitecture;
        public readonly bool runWith32BitShim;
        public readonly List<PluginData> plugins;

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneralPluginScanningContext"/> class.
        /// </summary>
        /// <param name="fileName">The file name of the plug-in.</param>
        /// <param name="platform">The processor architecture that the plug-in was built for.</param>
        /// <param name="logger">The logger.</param>
        public GeneralPluginScanningContext(string fileName, Architecture platform, IPluginLoadingLogger logger)
        {
            this.fileName = fileName;
            this.logger = logger;
            processorArchitecture = platform;
            plugins = [];
            runWith32BitShim = platform == Architecture.X86;
        }
    }
}
