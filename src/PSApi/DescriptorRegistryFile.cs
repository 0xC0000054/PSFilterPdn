using PaintDotNet.IO;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace PSFilterLoad.PSApi
{
    internal static class DescriptorRegistryFile
    {
        public static DescriptorRegistryValues Losd(string path)
        {
            Dictionary<string, DescriptorRegistryItem> persistentItems = new(StringComparer.Ordinal);
            bool isOldFormat = false;

            using (FileStream fs = new(path, FileMode.Open, FileAccess.Read))
            {
                IDictionary<string, DescriptorRegistryItem> values = null;

                if (DescriptorRegistryFileHeader.TryCreate(fs, out DescriptorRegistryFileHeader header))
                {
                    if (header.FileVersion == 2)
                    {
                        long dataLength = fs.Length - fs.Position;

                        byte[] data = new byte[dataLength];

                        fs.ProperRead(data, 0, data.Length);

                        using (MemoryStream ms = new(data))
                        {
                            values = PSFilterPdn.DataContractSerializerUtil.Deserialize<Dictionary<string, DescriptorRegistryItem>>(ms);
                        }
                    }
                }

                if (values != null && values.Count > 0)
                {
                    foreach (KeyValuePair<string, DescriptorRegistryItem> item in values)
                    {
                        persistentItems.Add(item.Key, item.Value);
                    }
                }
            }

            return new DescriptorRegistryValues(persistentItems, isOldFormat);
        }

        public static void Save(string path, DescriptorRegistryValues values)
        {
            if (values.Dirty)
            {
                using (FileStream fs = new(path, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    new DescriptorRegistryFileHeader().Save(fs);

                    using (MemoryStream ms = new())
                    {
                        PSFilterPdn.DataContractSerializerUtil.Serialize(ms, values.GetPersistedValuesReadOnly());

                        fs.Write(ms.GetBuffer(), 0, (int)ms.Length);
                    }
                }

                values.Dirty = false;
            }
        }

        private sealed class DescriptorRegistryFileHeader
        {
            // PFPR = PSFilterPdn registry
            private static readonly byte[] HeaderSignature = Encoding.UTF8.GetBytes("PFPR");

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
    }
}
