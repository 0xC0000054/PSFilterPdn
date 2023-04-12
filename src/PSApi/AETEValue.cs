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
    [KnownType(typeof(UnitFloat))]
    [KnownType(typeof(EnumeratedValue))]
    [KnownType(typeof(ActionDescriptorZString))]
    [Serializable]
    internal sealed class AETEValue
    {
        [DataMember]
        private uint type;
        [DataMember]
        private int flags;
        [DataMember]
        private int size;
        [DataMember]
        private object value;

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
    internal sealed class UnitFloat
    {
        [DataMember]
        private uint unit;
        [DataMember]
        private double value;

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
    internal sealed class EnumeratedValue
    {
        [DataMember]
        private uint type;
        [DataMember]
        private uint value;

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
    internal sealed class ActionDescriptorZString
    {
        [DataMember]
        private string value;

        public string Value => value;

        public ActionDescriptorZString(string value)
        {
            this.value = value;
        }
    }
}
