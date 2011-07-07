using System;
using System.Runtime.Serialization;

namespace PSFilterLoad.PSApi
{
    /// <summary>
    /// The struct that holds the saved filter parameter data.
    /// </summary>
    [Serializable]
    public sealed class GlobalParameters : ISerializable
    {

        private long parmDataSize;
        private byte[] parmDataBytes;
        private bool parmDataIsPSHandle;
        private long pluginDataSize;
        private byte[] pluginDataBytes;
        private bool pluginDataIsPSHandle;
        private int storeMethod;

        public byte[] ParmDataBytes
        {
            get
            {
                return parmDataBytes;
            }
            set
            {
                parmDataBytes = value;
            }
        }

        public long ParmDataSize
        {
            get
            {
                return parmDataSize;
            }
            set
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
            set
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
            set
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
            set
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
            set
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
            set
            {
                storeMethod = value;
            }
        }

        public static readonly GlobalParameters Empty = new GlobalParameters();

        public GlobalParameters()
        {
        }
        private GlobalParameters(SerializationInfo info, StreamingContext context)
        {
            this.parmDataSize = info.GetInt64("parmDataSize");
            this.parmDataBytes = (byte[])info.GetValue("parmDataBytes", typeof(byte[]));
            this.parmDataIsPSHandle = info.GetBoolean("parmDataIsPSHandle");
            this.pluginDataSize = info.GetInt64("pluginDataSize");
            this.pluginDataBytes = (byte[])info.GetValue("pluginDataBytes", typeof(byte[]));
            this.pluginDataIsPSHandle = info.GetBoolean("pluginDataIsPSHandle");
            this.storeMethod = info.GetInt32("storeMethod");
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("parmDataSize", this.parmDataSize);
            info.AddValue("parmDataBytes", this.parmDataBytes, typeof(byte[]));
            info.AddValue("parmDataIsPSHandle", this.parmDataIsPSHandle);
            info.AddValue("pluginDataSize", this.pluginDataSize);
            info.AddValue("pluginDataBytes", this.pluginDataBytes, typeof(byte[]));
            info.AddValue("pluginDataIsPSHandle", this.pluginDataIsPSHandle);
            info.AddValue("storeMethod", this.storeMethod);
        }
    }
}
