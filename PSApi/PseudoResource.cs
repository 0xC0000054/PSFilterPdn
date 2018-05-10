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

namespace PSFilterLoad.PSApi
{
    [Serializable]
    public sealed class PseudoResource
    {
        private uint key;
        private int index;
        private byte[] data;

        /// <summary>
        /// Gets the resource key.
        /// </summary>
        public uint Key
        {
            get
            {
                return key;
            }
        }

        /// <summary>
        /// Gets the resource index.
        /// </summary>
        public int Index
        {
            get
            {
                return index;
            }
            set
            {
                index = value;
            }
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
            return (key == otherKey && index == otherIndex);
        }
    }
}
