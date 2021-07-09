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

using System;
using System.Runtime.Serialization;

namespace PSFilterLoad.PSApi
{
    [DataContract]
    [Serializable]
    public sealed class PseudoResource
    {
#pragma warning disable IDE0044 // Add readonly modifier
        [DataMember]
        private uint key;
        [DataMember]
        private int index;
        [DataMember]
        private byte[] data;
#pragma warning restore IDE0044 // Add readonly modifier

        /// <summary>
        /// Gets the resource key.
        /// </summary>
        public uint Key => key;

        /// <summary>
        /// Gets the resource index.
        /// </summary>
        public int Index
        {
            get => index;
            set => index = value;
        }

        /// <summary>
        /// Gets the resource data.
        /// </summary>
        /// <returns></returns>
        public byte[] GetData()
        {
            return data;
        }

        public PseudoResource(uint key, int index, byte[] data)
        {
            this.key = key;
            this.index = index;
            this.data = data;
        }

        public bool Equals(uint otherKey, int otherIndex)
        {
            return key == otherKey && index == otherIndex;
        }
    }
}
