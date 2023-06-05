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

using System;
using System.Runtime.Serialization;

namespace PSFilterLoad.PSApi
{
    [DataContract]
    internal sealed class DescriptorSimpleReference
    {
        [DataMember(Order = 0)]
        private uint desiredClass;
        [DataMember(Order = 1)]
        private uint keyForm;
        [DataMember(Order = 2)]
        private byte[] name;
        [DataMember(Order = 3)]
        private int index;
        [DataMember(Order = 4)]
        private uint type;
        [DataMember(Order = 5)]
        private uint value;

        internal unsafe DescriptorSimpleReference(PIDescriptorSimpleReference* data)
        {
            desiredClass = data->desiredClass;
            keyForm = data->keyForm;
            name = GetNameBytes(data->keyData.name);
            index = data->keyData.index;
            type = data->keyData.type;
            value = data->keyData.value;
        }

        public unsafe void GetData(PIDescriptorSimpleReference* data)
        {
            data->desiredClass = desiredClass;
            data->keyForm = keyForm;

            Span<byte> nameField = new(data->keyData.name, 256);

            if (name is not null)
            {
                name.CopyTo(nameField);
            }
            else
            {
                nameField.Clear();
            }

            data->keyData.index = index;
            data->keyData.type = type;
            data->keyData.value = value;
        }

        private static unsafe byte[] GetNameBytes(byte* name)
        {
            byte[] bytes = null;

            // The name field is a Pascal-style string, the first byte gives the string length.
            if (name[0] != 0)
            {
                ReadOnlySpan<byte> nameData = new(name, name[0]);

                bytes = nameData.ToArray();
            }

            return bytes;
        }
    }
}
