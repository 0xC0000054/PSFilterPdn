using System;
using System.Runtime.Serialization;
using System.Security.Permissions;

namespace PSFilterLoad.PSApi
{
    /// <summary>
    /// The class that holds the saved filter global parameter data.
    /// </summary>
    [Serializable]
    public sealed class GlobalParameters : ISerializable
    {

        private long parameterDataSize;
        private byte[] parameterDataBytes;
        private bool parameterDataIsPSHandle;
        private long pluginDataSize;
        private byte[] pluginDataBytes;
        private bool pluginDataIsPSHandle;
        private int storeMethod;
       
        public byte[] GetParameterDataBytes()
        {
            return parameterDataBytes;
        }
        public void SetParameterDataBytes(byte[] value)
        {
            parameterDataBytes = value;
        }
 

        public long ParameterDataSize
        {
            get
            {
                return parameterDataSize;
            }
            set
            {
                parameterDataSize = value;
            }
        }

        /// <summary>
        /// Is the parm data a PS Handle.
        /// </summary>
        public bool ParameterDataIsPSHandle
        {
            get
            {
                return parameterDataIsPSHandle;
            }
            set
            {
                parameterDataIsPSHandle = value;
            }
        }

        public byte[] GetPluginDataBytes()
        {
            return pluginDataBytes;
        }
        public void SetPluginDataBytes(byte[] value)
        {
            pluginDataBytes = value;
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
        /// Is the plugin data a PS Handle.
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

        public GlobalParameters()
        {
            this.parameterDataSize = 0;
            this.parameterDataBytes = null;
            this.parameterDataIsPSHandle = false;
            this.pluginDataSize = 0;
            this.pluginDataBytes = null;
            this.pluginDataIsPSHandle = false;
            this.storeMethod = 0;
        }
        private GlobalParameters(SerializationInfo info, StreamingContext context)
        {
            this.parameterDataSize = info.GetInt64("parameterDataSize");
            this.parameterDataBytes = (byte[])info.GetValue("parameterDataBytes", typeof(byte[]));
            this.parameterDataIsPSHandle = info.GetBoolean("parameterDataIsPSHandle");
            this.pluginDataSize = info.GetInt64("pluginDataSize");
            this.pluginDataBytes = (byte[])info.GetValue("pluginDataBytes", typeof(byte[]));
            this.pluginDataIsPSHandle = info.GetBoolean("pluginDataIsPSHandle");
            this.storeMethod = info.GetInt32("storeMethod");
        }
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info", "info is null.");

            info.AddValue("parameterDataSize", this.parameterDataSize);
            info.AddValue("parameterDataBytes", this.parameterDataBytes, typeof(byte[]));
            info.AddValue("parameterDataIsPSHandle", this.parameterDataIsPSHandle);
            info.AddValue("pluginDataSize", this.pluginDataSize);
            info.AddValue("pluginDataBytes", this.pluginDataBytes, typeof(byte[]));
            info.AddValue("pluginDataIsPSHandle", this.pluginDataIsPSHandle);
            info.AddValue("storeMethod", this.storeMethod);
        }
    }
}
