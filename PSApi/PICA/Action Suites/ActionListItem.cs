/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2020 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace PSFilterLoad.PSApi.PICA
{
    [Serializable]
    public sealed class ActionListItem
    {
        private readonly uint type;
        private readonly object value;

        public uint Type => type;

        public object Value => value;

        public ActionListItem(uint type, object value)
        {
            this.type = type;
            this.value = value;
        }
    }
}
