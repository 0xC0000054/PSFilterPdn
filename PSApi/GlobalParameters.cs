/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2018 Nicholas Hayes
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
            get
            {
                return parameterDataStorageMethod;
            }
            set
            {
                parameterDataStorageMethod = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the parameter data memory must be executable.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the parameter data memory must be executable; otherwise, <c>false</c>.
        /// </value>
        public bool ParameterDataExecutable
        {
            get
            {
                return parameterDataExecutable;
            }
            set
            {
                parameterDataExecutable = value;
            }
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
            get
            {
                return pluginDataStorageMethod;
            }
            set
            {
                pluginDataStorageMethod = value;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether plugin data memory must be executable.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the plugin data memory must be executable; otherwise, <c>false</c>.
        /// </value>
        public bool PluginDataExecutable
        {
            get
            {
                return pluginDataExecutable;
            }
            set
            {
                pluginDataExecutable = value;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has data.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance has data; otherwise, <c>false</c>.
        /// </value>
        public bool HasData
        {
            get
            {
                // Only the filters that use the FilterRecord 'parameters' handle are supported.
                // A plug-in that only uses the 'data' handle and does not use scripting will
                // reshow its dialog when Paint.NET executes the 'Repeat Effect' command.
                //
                // This is due to the fact that Filter Factory-based filters will crash with an
                // access violation when the 32-bit proxy process restores the 'data' handle.
                // It is possible that Filter Factory stores a pointer in its data handle and the
                // crash is caused by the pointer becoming invalid due to the address space layout
                // changing when the proxy process is launched a second time.
                //
                // Unfortunately the Paint.NET Effects API does not have a mechanism to allow the
                // proxy process to stay resident in the background for Paint.NET's lifetime,
                // which would fix this issue.

                return (parameterDataBytes != null);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GlobalParameters"/> class.
        /// </summary>
        public GlobalParameters()
        {
            this.parameterDataBytes = null;
            this.parameterDataStorageMethod = DataStorageMethod.HandleSuite;
            this.parameterDataExecutable = false;
            this.pluginDataBytes = null;
            this.pluginDataStorageMethod = DataStorageMethod.HandleSuite;
            this.pluginDataExecutable = false;
        }
        private GlobalParameters(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));

            this.parameterDataBytes = (byte[])info.GetValue("parameterDataBytes", typeof(byte[]));
            this.parameterDataStorageMethod = (DataStorageMethod)info.GetValue("parameterDataStorageMethod", typeof(DataStorageMethod));
            this.parameterDataExecutable = info.GetBoolean("parameterDataExecutable");

            this.pluginDataBytes = (byte[])info.GetValue("pluginDataBytes", typeof(byte[]));
            this.pluginDataStorageMethod = (DataStorageMethod)info.GetValue("pluginDataStorageMethod", typeof(DataStorageMethod));
            this.pluginDataExecutable = info.GetBoolean("pluginDataExecutable");
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
                throw new ArgumentNullException(nameof(info));

            info.AddValue("parameterDataBytes", this.parameterDataBytes, typeof(byte[]));
            info.AddValue("parameterDataStorageMethod", this.parameterDataStorageMethod, typeof(DataStorageMethod));
            info.AddValue("parameterDataExecutable", this.parameterDataExecutable);

            info.AddValue("pluginDataBytes", this.pluginDataBytes, typeof(byte[]));
            info.AddValue("pluginDataStorageMethod", this.pluginDataStorageMethod, typeof(DataStorageMethod));
            info.AddValue("pluginDataExecutable", this.pluginDataExecutable);
        }
    }
}
