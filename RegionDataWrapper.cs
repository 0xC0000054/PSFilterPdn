/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2022 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System.Drawing.Drawing2D;
using System.Runtime.Serialization;

namespace PSFilterPdn
{
    [DataContract]
    public sealed class RegionDataWrapper
    {
        [DataMember]
        private byte[] rgnData;

        internal RegionDataWrapper(RegionData rgn)
        {
            rgnData = rgn.Data;
        }

        public byte[] GetData()
        {
            return rgnData;
        }
    }
}
