using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    internal delegate int filterep(short selector, [In, Out]IntPtr fpb, ref IntPtr pluginData, ref short result);
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
    }

    internal struct PIEntrypoint
    {
        /// <summary>
        /// The pointer to the dll module handle
        /// </summary>
        public IntPtr dll;
        /// <summary>
        /// The entrypoint for the FilterParmBlock and AboutRecord
        /// </summary>
        public filterep entry;
    }

}
