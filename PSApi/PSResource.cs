using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PSFilterLoad.PSApi
{
    [Serializable]
    public sealed class PSResource
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

        public PSResource(uint key, int index, byte[] data)
        {
            this.key = key;
            this.index = index;
            this.data = data;
        }


        public bool Equals(uint otherKey, int otherIndex)
        {
            return (this.key == otherKey && this.index == otherIndex);
        }
    }
}
