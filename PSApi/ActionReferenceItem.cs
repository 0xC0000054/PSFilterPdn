/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2018 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace PSFilterLoad.PSApi
{
    [Serializable]
    public enum ActionReferenceForm : uint
    {
        Class = 0x436C7373,
        Enumerated = 0x456E6D72,
        Identifier = 0x49646E74,
        Index = 0x696E6478,
        Offset = 0x72656C65,
        Property = 0x70726F70,
        Name = 0x6E616D65
    }

    [Serializable]
    public sealed class ActionReferenceItem
    {
        private readonly ActionReferenceForm form;
        private readonly uint desiredClass;
        private readonly object value;

        public ActionReferenceForm Form
        {
            get
            {
                return this.form;
            }
        }

        public uint DesiredClass
        {
            get
            {
                return this.desiredClass;
            }
        }

        public object Value
        {
            get
            {
                return this.value;
            }
        }

        public ActionReferenceItem(ActionReferenceForm form, uint desiredClass, object value)
        {
            this.form = form;
            this.desiredClass = desiredClass;
            this.value = value;
        }
    }
}
