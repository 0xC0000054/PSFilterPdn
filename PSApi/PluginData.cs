using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl, CharSet = CharSet.Ansi), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate int filterep(short selector, IntPtr pluginParamBlock, ref IntPtr pluginData, ref short result);
    /// <summary>
    /// The class that encapsulates a Photoshop Filter plugin
    /// </summary>
    internal sealed class PluginData
    {
        public string fileName;
        public string entryPoint;
        public string category;
        public string title;
        public FilterCaseInfo[] filterInfo;
        /// <summary>
        /// The structure containing the dll entrypoint
        /// </summary>
        public PIEntrypoint entry;
        /// <summary>
        /// Used to run 32-bit plugins in 64-bit Paint.NET
        /// </summary>
        public bool runWith32BitShim;
        public AETEData aete;
    }

    internal struct PIEntrypoint
    {
        /// <summary>
        /// The pointer to the dll module handle
        /// </summary>
        public SafeLibraryHandle dll;
        /// <summary>
        /// The entrypoint for the FilterParmBlock and AboutRecord
        /// </summary>
        public filterep entry;
    }

}
