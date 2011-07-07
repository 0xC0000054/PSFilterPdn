using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PSFilterLoad.PSApi
{
#if PSSDK4
    [Serializable]
	public struct AETEValue
    {
        public uint type;
        public int flags;
        public int size;
        public object value;

        public AETEValue(uint type, int flags, int size, object value)
        {
            this.type = type;
            this.flags = flags;
            this.size = size;
            this.value = value;
        }
    } 
  
#endif
}
