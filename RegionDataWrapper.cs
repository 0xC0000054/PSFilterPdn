using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Serialization;

namespace PSFilterPdn
{
    [Serializable]
    public sealed class RegionDataWrapper : ISerializable
    {
        private byte[] rgnData;

        internal RegionDataWrapper(RegionData rgn)
        {
            this.rgnData = rgn.Data;
        }

        public RegionDataWrapper(SerializationInfo info, StreamingContext context)
        {
            this.rgnData = (byte[])info.GetValue("rgnData", typeof(byte[]));
        }

        public byte[] Data
        {
            get
            {
                return rgnData;
            }
        }


        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("rgnData", this.rgnData, typeof(byte[]));
        }
    }
}
