/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2017 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Drawing.Drawing2D;
using System.Runtime.Serialization;
using System.Security.Permissions;

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

        private RegionDataWrapper(SerializationInfo info, StreamingContext context)
        {
            this.rgnData = (byte[])info.GetValue("rgnData", typeof(byte[]));
        }

        public byte[] GetData()
        {
            return rgnData;
        }

        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue("rgnData", this.rgnData, typeof(byte[]));
        }
    }
}
