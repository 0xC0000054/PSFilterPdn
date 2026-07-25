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

using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi.Loader.PIPL
{
    internal sealed class BinaryPIPLPluginScanningContext
    {
        public readonly GeneralPluginScanningContext general;
        public readonly uint entryPointPropertyID;

        /// <summary>
        /// Initializes a new instance of the <see cref="BinaryPIPLPluginScanningContext"/> class.
        /// </summary>
        /// <param name="general">The general.</param>
        /// <exception cref="ArgumentNullException"><paramref name="general"/> is <see langword="null"/>.</exception>
        /// <exception cref="PlatformNotSupportedException">The plugin's processor architecture is not supported.</exception>
        public BinaryPIPLPluginScanningContext(GeneralPluginScanningContext general)
        {
            this.general = general ?? throw new ArgumentNullException(nameof(general));

            switch (general.processorArchitecture)
            {
                case Architecture.X86:
                    entryPointPropertyID = PIPropertyID.PIWin32X86CodeProperty;
                    break;
                case Architecture.X64:
                    entryPointPropertyID = PIPropertyID.PIWin64X86CodeProperty;
                    break;
                case Architecture.Arm64:
                    entryPointPropertyID = PIPropertyID.PIWin64ARMCodeProperty;
                    break;
                default:
                    throw new PlatformNotSupportedException($"No platform entry point was defined for {nameof(Architecture)}.{general.processorArchitecture}.");
            }
        }
    }
}
