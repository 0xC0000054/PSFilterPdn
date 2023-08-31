/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2023 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using MessagePack;

namespace PSFilterLoad.PSApi
{
    /// <summary>
    /// The class that encapsulates the filter's global parameter data.
    /// </summary>
    [MessagePackObject]
    [MessagePackFormatter(typeof(Formatter))]
    internal sealed partial class GlobalParameters
    {
        internal enum DataStorageMethod : int
        {
            HandleSuite,
            OTOFHandle,
            RawBytes
        }

#pragma warning disable IDE0032 // Use auto property
        private byte[]? parameterDataBytes;
        private int parameterDataStorageMethod;
        private bool parameterDataExecutable;
        private byte[]? pluginDataBytes;
        private int pluginDataStorageMethod;
        private bool pluginDataExecutable;
#pragma warning restore IDE0032 // Use auto property

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalParameters"/> class.
        /// </summary>
        internal GlobalParameters()
        {
            parameterDataBytes = null;
            ParameterDataStorageMethod = DataStorageMethod.HandleSuite;
            parameterDataExecutable = false;
            pluginDataBytes = null;
            PluginDataStorageMethod = DataStorageMethod.HandleSuite;
        }

        private GlobalParameters(byte[]? parameterDataBytes,
                                 int parameterDataStorageMethod,
                                 bool parameterDataExecutable,
                                 byte[]? pluginDataBytes,
                                 int pluginDataStorageMethod,
                                 bool pluginDataExecutable)
        {
            this.parameterDataBytes = parameterDataBytes;
            this.parameterDataStorageMethod = parameterDataStorageMethod;
            this.parameterDataExecutable = parameterDataExecutable;
            this.pluginDataBytes = pluginDataBytes;
            this.pluginDataStorageMethod = pluginDataStorageMethod;
            this.pluginDataExecutable = pluginDataExecutable;
        }

        /// <summary>
        /// Gets the parameter data bytes.
        /// </summary>
        /// <returns>The parameter data bytes.</returns>
        public byte[]? GetParameterDataBytes()
        {
            return parameterDataBytes;
        }

        /// <summary>
        /// Sets the parameter data bytes.
        /// </summary>
        /// <param name="data">The data.</param>
        public void SetParameterDataBytes(byte[] data)
        {
            parameterDataBytes = data;
        }

        /// <summary>
        /// Gets or sets the storage method of the parameter data.
        /// </summary>
        /// <value>
        /// The parameter data storage method.
        /// </value>
        public DataStorageMethod ParameterDataStorageMethod
        {
            get => (DataStorageMethod)parameterDataStorageMethod;
            set => parameterDataStorageMethod = (int)value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the parameter data memory must be executable.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the parameter data memory must be executable; otherwise, <c>false</c>.
        /// </value>
        public bool ParameterDataExecutable
        {
            get => parameterDataExecutable;
            set => parameterDataExecutable = value;
        }

        /// <summary>
        /// Gets the plugin data bytes.
        /// </summary>
        /// <returns>The plugin data bytes.</returns>
        public byte[]? GetPluginDataBytes()
        {
            return pluginDataBytes;
        }

        /// <summary>
        /// Sets the plugin data bytes.
        /// </summary>
        /// <param name="data">The data.</param>
        public void SetPluginDataBytes(byte[] data)
        {
            pluginDataBytes = data;
        }

        /// <summary>
        /// Gets or sets the storage method of the plugin data.
        /// </summary>
        /// <value>
        /// The plugin data storage method.
        /// </value>
        public DataStorageMethod PluginDataStorageMethod
        {
            get => (DataStorageMethod)pluginDataStorageMethod;
            set => pluginDataStorageMethod = (int)value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether plugin data memory must be executable.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the plugin data memory must be executable; otherwise, <c>false</c>.
        /// </value>
        public bool PluginDataExecutable
        {
            get => pluginDataExecutable;
            set => pluginDataExecutable = value;
        }
    }
}
