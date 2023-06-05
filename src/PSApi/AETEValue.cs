﻿/////////////////////////////////////////////////////////////////////////////////
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

using PSFilterLoad.PSApi.PICA;
using System;
using System.Runtime.Serialization;

namespace PSFilterLoad.PSApi
{
    [DataContract]
    [KnownType(typeof(UnitFloat))]
    [KnownType(typeof(EnumeratedValue))]
    [KnownType(typeof(ActionDescriptorZString))]
    [KnownType(typeof(DescriptorSimpleReference))]
    [Serializable]
    internal sealed class AETEValue
    {
        [DataMember]
        private uint type;
        [DataMember]
        private object value;

        public uint Type => type;

        public object Value => value;

        public AETEValue(uint type, object value)
        {
            this.type = type;
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
}
