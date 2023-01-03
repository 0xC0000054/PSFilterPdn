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
using System.Collections.Generic;

namespace PSFilterLoad.PSApi.PICA
{
    [Serializable]
    internal sealed class ActionListDescriptor
    {
        private readonly uint type;
        private readonly Dictionary<uint, AETEValue> descriptorValues;

        public uint Type => type;

        public Dictionary<uint, AETEValue> DescriptorValues => descriptorValues;

        public ActionListDescriptor(uint type, Dictionary<uint, AETEValue> descriptorValues)
        {
            this.type = type;
            this.descriptorValues = descriptorValues;
        }
    }
}
