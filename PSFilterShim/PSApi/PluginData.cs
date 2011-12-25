using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace PSFilterLoad.PSApi
{
    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl, CharSet = CharSet.Ansi), System.Security.SuppressUnmanagedCodeSecurity]
    internal delegate int filterep(short selector, IntPtr pluginParamBlock, ref IntPtr pluginData, ref short result);
 
#pragma warning disable 0649
   
    /// <summary>
    /// The class that encapsulates a Photoshop Filter plugin
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
        /// The structure containing the dll entrypoint
        /// </summary>
        public PIEntrypoint entry;
        /// <summary>
        /// Used to run 32-bit plugins in 64-bit Paint.NET
        /// </summary>
        public bool runWith32BitShim;
        [DataMember]
        public AETEData aete;
    }
#pragma warning restore

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
