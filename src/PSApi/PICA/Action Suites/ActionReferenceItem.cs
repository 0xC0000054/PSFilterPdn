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

namespace PSFilterLoad.PSApi.PICA
{
    internal static class ActionReferenceForm
    {
        internal const uint Class = 0x436C7373;
        internal const uint Enumerated = 0x456E6D72;
        internal const uint Identifier = 0x49646E74;
        internal const uint Index = 0x696E6478;
        internal const uint Offset = 0x72656C65;
        internal const uint Property = 0x70726F70;
        internal const uint Name = 0x6E616D65;
    }

    [Serializable]
    internal sealed class ActionReferenceItem
    {
        private readonly uint form;
        private readonly uint desiredClass;
        private readonly object value;

        public uint Form => form;

        public uint DesiredClass => desiredClass;

        public object Value => value;

        public ActionReferenceItem(uint form, uint desiredClass, object value)
        {
            this.form = form;
            this.desiredClass = desiredClass;
            this.value = value;
        }
    }
}
