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

using System.Runtime.Serialization;

namespace PSFilterLoad.PSApi
{
    /// <summary>
    /// The class that holds the saved filter global parameter data.
    /// </summary>
    [DataContract]
    internal sealed class GlobalParameters
    {
        public enum DataStorageMethod
        {
            HandleSuite,
            OTOFHandle,
            RawBytes
        }

        [DataMember]
        private byte[] parameterDataBytes;
        [DataMember]
        private DataStorageMethod parameterDataStorageMethod;
        [DataMember]
        private bool parameterDataExecutable;
        [DataMember]
        private byte[] pluginDataBytes;
        [DataMember]
        private DataStorageMethod pluginDataStorageMethod;
        [DataMember]
        private bool pluginDataExecutable;

        /// <summary>
        /// Gets the parameter data bytes.
        /// </summary>
        /// <returns>The parameter data bytes.</returns>
        public byte[] GetParameterDataBytes()
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
            get => parameterDataStorageMethod;
            set => parameterDataStorageMethod = value;
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
        public byte[] GetPluginDataBytes()
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
            get => pluginDataStorageMethod;
            set => pluginDataStorageMethod = value;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalParameters"/> class.
        /// </summary>
        public GlobalParameters()
        {
            parameterDataBytes = null;
            parameterDataStorageMethod = DataStorageMethod.HandleSuite;
            parameterDataExecutable = false;
            pluginDataBytes = null;
            pluginDataStorageMethod = DataStorageMethod.HandleSuite;
            pluginDataExecutable = false;
        }
    }
}
