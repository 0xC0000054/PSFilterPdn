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

using PSFilterPdn.Interop;

namespace PSFilterLoad.PSApi
{
    internal static class ProcessInformation
    {
        /// <summary>
        /// Gets the processor architecture of the current process.
        /// </summary>
        /// <value>
        /// The processor architecture of the current process.
        /// </value>
        public static ProcessorArchitecture Architecture { get; } = GetProcessorArchitecture();

        private static ProcessorArchitecture GetProcessorArchitecture()
        {
            NativeStructs.SYSTEM_INFO info = new NativeStructs.SYSTEM_INFO();

            SafeNativeMethods.GetSystemInfo(ref info);

            ProcessorArchitecture architecture;

            switch (info.wProcessorArchitecture)
            {
                case NativeConstants.PROCESSOR_ARCHITECTURE_INTEL:
                    architecture = ProcessorArchitecture.X86;
                    break;
                case NativeConstants.PROCESSOR_ARCHITECTURE_AMD64:
                    architecture = ProcessorArchitecture.X64;
                    break;
                case NativeConstants.PROCESSOR_ARCHITECTURE_ARM:
                    architecture = ProcessorArchitecture.Arm;
                    break;
                case NativeConstants.PROCESSOR_ARCHITECTURE_ARM64:
                    architecture = ProcessorArchitecture.Arm64;
                    break;
                default:
                    architecture = ProcessorArchitecture.Unknown;
                    break;
            }

            return architecture;
        }
    }
}
