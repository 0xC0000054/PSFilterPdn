using System;

namespace PSFilterLoad.PSApi
{
    [Serializable]
	public struct AETEValue
    {
        private uint type;
        private int flags;
        private int size;
        private object value;

        public uint Type
        {
            get
            {
                return type;
            }
        }

        public int Flags
        {
            get
            {
                return flags;
            }
        }

        public int Size
        {
            get
            {
                return size;
            }
        }

        public object Value
        {
            get
            {
                return value;
            }
        }

        public AETEValue(uint type, int flags, int size, object value)
        {
            this.type = type;
            this.flags = flags;
            this.size = size;
            this.value = value;
        }
    } 
  
}
