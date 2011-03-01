using System;
using System.Runtime.Serialization;

namespace PSFilterLoad.PSApi
{
    /// <summary>
    /// The class that holds the saved filter parameter data.
    /// </summary>
    internal sealed class ParameterData
    {
        
        private long parmDataSize;
        private byte[] paramDataBytes;
        private bool parmDataIsPSHandle;
        private long pluginDataSize;
        private byte[] pluginDataBytes;
        private bool pluginDataIsPSHandle;
        private int storeMethod;

        public byte[] ParmDataBytes
        {
            get
            {
                return paramDataBytes;
            }
            internal set
            {
                paramDataBytes = value;
            }
        }

        public long ParmDataSize
        {
            get
            {
                return parmDataSize;
            }
            internal set 
            {
                parmDataSize = value;
            }
        }

        /// <summary>
        /// Is the parm data a PS Handle (OTOF signature).
        /// </summary>
        public bool ParmDataIsPSHandle
        {
            get
            {
                return parmDataIsPSHandle;
            }
            internal set
            {
                parmDataIsPSHandle = value;
            }
        }
      
        public byte[] PluginDataBytes
        {
            get
            {
                return pluginDataBytes;
            }
            internal set
            {
                pluginDataBytes = value;
            }
        }

        public long PluginDataSize
        {
            get
            {
                return pluginDataSize;
            }
            internal set
            {
                pluginDataSize = value;
            }
        }

        /// <summary>
        /// Is the plugin data a PS Handle (OTOF signature).
        /// </summary>
        public bool PluginDataIsPSHandle
        {
            get
            {
                return pluginDataIsPSHandle;
            }
            internal set
            {
                pluginDataIsPSHandle = value;
            }
        }

        public int StoreMethod
        { 
            get 
            {
                return storeMethod;
            }
            internal set
            {
                storeMethod = value;
            }
        }

        public static readonly ParameterData Empty = new ParameterData();


    }
}
