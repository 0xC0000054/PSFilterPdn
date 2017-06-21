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
using System.Collections.Generic;

namespace PSFilterLoad.PSApi
{
    [Serializable]
    public sealed class ActionListDescriptor
    {
        private readonly uint type;
        private readonly Dictionary<uint, AETEValue> descriptorValues;

        public uint Type
        {
            get
            {
                return this.type;
            }
        }

        public Dictionary<uint, AETEValue> DescriptorValues
        {
            get
            {
                return this.descriptorValues;
            }
        }

        public ActionListDescriptor(uint type, Dictionary<uint, AETEValue> descriptorValues)
        {
            this.type = type;
            this.descriptorValues = descriptorValues;
        }
    }
}
