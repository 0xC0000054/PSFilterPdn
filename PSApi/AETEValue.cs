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

using System;
using System.Runtime.Serialization;

namespace PSFilterLoad.PSApi
{
    [DataContract]
    [KnownType(typeof(UnitFloat))]
    [KnownType(typeof(EnumeratedValue))]
    [KnownType(typeof(ActionDescriptorZString))]
    [Serializable]
    public sealed class AETEValue
    {
#pragma warning disable IDE0044 // Add readonly modifier
        [DataMember]
        private uint type;
        [DataMember]
        private int flags;
        [DataMember]
        private int size;
        [DataMember]
        private object value;
#pragma warning restore IDE0044 // Add readonly modifier

        public uint Type => type;

        public int Flags => flags;

        public int Size => size;

        public object Value => value;

        public AETEValue(uint type, int flags, int size, object value)
        {
            this.type = type;
            this.flags = flags;
            this.size = size;
            this.value = value;
        }
    }

    [DataContract]
    [Serializable]
    public sealed class UnitFloat
    {
#pragma warning disable IDE0044 // Add readonly modifier
        [DataMember]
        private uint unit;
        [DataMember]
        private double value;
#pragma warning restore IDE0044 // Add readonly modifier

        public uint Unit => unit;

        public double Value => value;

        public UnitFloat(uint unit, double value)
        {
            this.unit = unit;
            this.value = value;
        }
    }

    [DataContract]
    [Serializable]
    public sealed class EnumeratedValue
    {
#pragma warning disable IDE0044 // Add readonly modifier
        [DataMember]
        private uint type;
        [DataMember]
        private uint value;
#pragma warning restore IDE0044 // Add readonly modifier

        public uint Type => type;

        public uint Value => value;

        public EnumeratedValue(uint type, uint value)
        {
            this.type = type;
            this.value = value;
        }
    }

    [DataContract]
    [Serializable]
    public sealed class ActionDescriptorZString
    {
#pragma warning disable IDE0044 // Add readonly modifier
        [DataMember]
        private string value;
#pragma warning restore IDE0044 // Add readonly modifier

        public string Value => value;

        public ActionDescriptorZString(string value)
        {
            this.value = value;
        }
    }
}
