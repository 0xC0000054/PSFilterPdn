/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2021 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PaintDotNet.IO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace PSFilterLoad.PSApi
{
    [Serializable]
    public sealed class DescriptorRegistryValues
    {
        private readonly ReadOnlyDictionary<string, DescriptorRegistryItem> persistedValues;
        private readonly ReadOnlyDictionary<string, DescriptorRegistryItem> sessionValues;
        private bool dirty;

        /// <summary>
        /// Initializes a new instance of the <see cref="DescriptorRegistryValues"/> class.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
        internal DescriptorRegistryValues(string path)
        {
            if (path is null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            Dictionary<string, DescriptorRegistryItem> persistentItems = new Dictionary<string, DescriptorRegistryItem>(StringComparer.Ordinal);
            Dictionary<string, DescriptorRegistryItem> sessionItems = new Dictionary<string, DescriptorRegistryItem>(StringComparer.Ordinal);
            bool isOldFormat = false;

            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                ReadOnlyDictionary<string, DescriptorRegistryItem> values = null;

                if (DescriptorRegistryFileHeader.TryCreate(fs, out DescriptorRegistryFileHeader header))
                {
                    if (header.FileVersion == 1)
                    {
                        long dataLength = fs.Length - fs.Position;

                        byte[] data = new byte[dataLength];

                        fs.ProperRead(data, 0, data.Length);

                        using (MemoryStream ms = new MemoryStream(data))
                        {
                            values = PSFilterPdn.DataContractSerializerUtil.Deserialize<ReadOnlyDictionary<string, DescriptorRegistryItem>>(ms);
                        }
                    }
                }
                else
                {
                    fs.Position = 0;

                    SelfBinder binder = new SelfBinder();
                    BinaryFormatter bf = new BinaryFormatter() { Binder = binder };
                    values = (ReadOnlyDictionary<string, DescriptorRegistryItem>)bf.Deserialize(fs);
                    isOldFormat = true;
                }

                if (values != null && values.Count > 0)
                {
                    foreach (KeyValuePair<string, DescriptorRegistryItem> item in values)
                    {
                        persistentItems.Add(item.Key, item.Value);
                    }
                }
            }

            persistedValues = new ReadOnlyDictionary<string, DescriptorRegistryItem>(persistentItems);
            sessionValues = new ReadOnlyDictionary<string, DescriptorRegistryItem>(sessionItems);
            dirty = isOldFormat;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DescriptorRegistryValues"/> class.
        /// </summary>
        /// <param name="values">The registry values.</param>
        /// <param name="persistentValuesChanged"><c>true</c> if the persistent values have been changed; otherwise, <c>false</c>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="values"/> is null.</exception>
        public DescriptorRegistryValues(IDictionary<string, DescriptorRegistryItem> values, bool persistentValuesChanged)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            Dictionary<string, DescriptorRegistryItem> persistentItems = new Dictionary<string, DescriptorRegistryItem>(StringComparer.Ordinal);
            Dictionary<string, DescriptorRegistryItem> sessionItems = new Dictionary<string, DescriptorRegistryItem>(StringComparer.Ordinal);

            foreach (KeyValuePair<string, DescriptorRegistryItem> item in values)
            {
                if (item.Value.IsPersistent)
                {
                    persistentItems.Add(item.Key, item.Value);
                }
                else
                {
                    sessionItems.Add(item.Key, item.Value);
                }
            }

            persistedValues = new ReadOnlyDictionary<string, DescriptorRegistryItem>(persistentItems);
            sessionValues = new ReadOnlyDictionary<string, DescriptorRegistryItem>(sessionItems);
            dirty = persistentValuesChanged;
        }

        /// <summary>
        /// Gets the values that are persisted between host sessions.
        /// </summary>
        /// <value>
        /// The values that are persisted between host sessions.
        /// </value>
        public ReadOnlyDictionary<string, DescriptorRegistryItem> PersistedValues => persistedValues;

        /// <summary>
        /// Gets the values that are stored for the current session.
        /// </summary>
        /// <value>
        /// The values that are stored for the current session.
        /// </value>
        public ReadOnlyDictionary<string, DescriptorRegistryItem> SessionValues => sessionValues;

        /// <summary>
        /// Gets or sets a value indicating whether the persisted settings have been marked as changed.
        /// </summary>
        /// <value>
        ///   <c>true</c> if the persisted settings have changed; otherwise, <c>false</c>.
        /// </value>
        public bool Dirty
        {
            get => dirty;
            set => dirty = value;
        }

        /// <summary>
        /// Saves the persisted values.
        /// </summary>
        /// <param name="path">The file path.</param>
        internal void SavePersistedValues(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                new DescriptorRegistryFileHeader().Save(fs);

                using (MemoryStream ms = new MemoryStream())
                {
                    PSFilterPdn.DataContractSerializerUtil.Serialize(ms, persistedValues);

                    fs.Write(ms.GetBuffer(), 0, (int)ms.Length);
                }
            }
        }

        private sealed class DescriptorRegistryFileHeader
        {
            // PFPR = PSFilterPdn registry
            private static readonly byte[] HeaderSignature = System.Text.Encoding.UTF8.GetBytes("PFPR");

            public DescriptorRegistryFileHeader()
            {
                FileVersion = 1;
            }

            private DescriptorRegistryFileHeader(int fileVersion)
            {
                FileVersion = fileVersion;
            }

            public int FileVersion { get; }

            public static bool TryCreate(Stream stream, out DescriptorRegistryFileHeader header)
            {
                header = null;

                bool result = false;

                if (stream.Length > 8)
                {
                    byte[] headerBytes = new byte[8];

                    stream.ProperRead(headerBytes, 0, headerBytes.Length);

                    if (CheckHeaderSignature(headerBytes))
                    {
                        header = new DescriptorRegistryFileHeader(BitConverter.ToInt32(headerBytes, 4));
                        result = true;
                    }
                }

                return result;
            }

            public void Save(Stream stream)
            {
                stream.Write(HeaderSignature, 0, HeaderSignature.Length);
                stream.Write(BitConverter.GetBytes(FileVersion), 0, 4);
            }

            private static bool CheckHeaderSignature(byte[] bytes)
            {
                if (bytes.Length < 4)
                {
                    return false;
                }

                for (int i = 0; i < 4; i++)
                {
                    if (bytes[i] != HeaderSignature[i])
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Binds the serialization to types in the currently loaded assembly.
        /// </summary>
        private sealed class SelfBinder : System.Runtime.Serialization.SerializationBinder
        {
            public override Type BindToType(string assemblyName, string typeName)
            {
                return Type.GetType(string.Format(CultureInfo.InvariantCulture, "{0},{1}", typeName, assemblyName));
            }
        }
    }
}
