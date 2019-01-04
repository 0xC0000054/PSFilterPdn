/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2019 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace PSFilterLoad.PSApi.PICA
{
    [Serializable]
    public sealed class ActionListDescriptor
    {
        private readonly uint type;
        private readonly ReadOnlyDictionary<uint, AETEValue> descriptorValues;

        public uint Type
        {
            get
            {
                return type;
            }
        }

        public ReadOnlyDictionary<uint, AETEValue> DescriptorValues
        {
            get
            {
                return descriptorValues;
            }
        }

        public ActionListDescriptor(uint type, ReadOnlyDictionary<uint, AETEValue> descriptorValues)
        {
            this.type = type;
            this.descriptorValues = descriptorValues;
        }
    }
}
