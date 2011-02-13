using System;

namespace PSFilterLoad.PSApi
{
    /// <summary>
    /// The struct that holds the saved filter parameter data.
    /// </summary>
    public sealed class ParameterData : IDisposable
    {
        
        private IntPtr parmHandle;
        private long handleSize;
        private byte[] paramDataBytes;
        private bool parmDataIsPSHandle;
        private IntPtr pluginData;
        private long pluginDataSize;
        private byte[] pluginDataBytes;
        private bool pluginDataIsPSHandle;
        private int storeMethod;

	    public IntPtr ParmHandle
	    {
		    get 
            { 
                return parmHandle;
            }
		    internal set 
            { 
                parmHandle = value;
            }
	    }

        public long HandleSize
        {
            get
            {
                return handleSize;
            }
            internal set 
            {
                handleSize = value;
            }
        }

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


        public IntPtr PluginData
        {
            get 
            {
                return pluginData;
            }
            internal set
            {
                pluginData = value;
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

        #region IDisposable Members
        private bool disposed;
        public void Dispose()
        {
            if (!disposed)
            {
                parmHandle = IntPtr.Zero;
                pluginData = IntPtr.Zero;
                disposed = true;
            }
        }

        #endregion
    }
}
