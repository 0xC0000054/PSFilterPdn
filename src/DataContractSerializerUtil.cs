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

using System.IO;
using System.Runtime.Serialization;

namespace PSFilterPdn
{
    public static class DataContractSerializerUtil
    {
        public static T Deserialize<T>(string path)
        {
            T obj = default;

            using (FileStream fs = new(path, FileMode.Open, FileAccess.Read))
            {
                obj = Deserialize<T>(fs);
            }

            return obj;
        }

        public static T Deserialize<T>(Stream stream)
        {
            return (T)new DataContractSerializer(typeof(T)).ReadObject(stream);
        }

        public static void Serialize<T>(string path, T obj)
        {
            using (FileStream fs = new(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                Serialize(fs, obj);
            }
        }

        public static void Serialize<T>(Stream stream, T obj)
        {
            new DataContractSerializer(typeof(T)).WriteObject(stream, obj);
        }
    }
}
