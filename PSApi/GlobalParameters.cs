/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2019 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

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
        public enum DataStorageMethod
        {
            HandleSuite,
            OTOFHandle,
            RawBytes
        }

        private byte[] parameterDataBytes;
        private DataStorageMethod parameterDataStorageMethod;
        private bool parameterDataExecutable;
        private byte[] pluginDataBytes;
        private DataStorageMethod pluginDataStorageMethod;
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
        private GlobalParameters(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            parameterDataBytes = (byte[])info.GetValue("parameterDataBytes", typeof(byte[]));
            parameterDataStorageMethod = (DataStorageMethod)info.GetValue("parameterDataStorageMethod", typeof(DataStorageMethod));
            parameterDataExecutable = info.GetBoolean("parameterDataExecutable");

            pluginDataBytes = (byte[])info.GetValue("pluginDataBytes", typeof(byte[]));
            pluginDataStorageMethod = (DataStorageMethod)info.GetValue("pluginDataStorageMethod", typeof(DataStorageMethod));
            pluginDataExecutable = info.GetBoolean("pluginDataExecutable");
        }
        /// <summary>
        /// Populates a <see cref="T:System.Runtime.Serialization.SerializationInfo"/> with the data needed to serialize the target object.
        /// </summary>
        /// <param name="info">The <see cref="T:System.Runtime.Serialization.SerializationInfo"/> to populate with data.</param>
        /// <param name="context">The destination (see <see cref="T:System.Runtime.Serialization.StreamingContext"/>) for this serialization.</param>
        /// <exception cref="T:System.Security.SecurityException">
        /// The caller does not have the required permission.
        ///   </exception>
        ///   <exception cref="T:System.ArgumentNullException">The SerializationInfo is null.</exception>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue("parameterDataBytes", parameterDataBytes, typeof(byte[]));
            info.AddValue("parameterDataStorageMethod", parameterDataStorageMethod, typeof(DataStorageMethod));
            info.AddValue("parameterDataExecutable", parameterDataExecutable);

            info.AddValue("pluginDataBytes", pluginDataBytes, typeof(byte[]));
            info.AddValue("pluginDataStorageMethod", pluginDataStorageMethod, typeof(DataStorageMethod));
            info.AddValue("pluginDataExecutable", pluginDataExecutable);
        }
    }
}
