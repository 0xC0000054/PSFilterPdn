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
using System.Drawing;
using System.Runtime.Serialization;

namespace PSFilterPdn
{
    [DataContract]
    public sealed class PSFilterShimData
    {
        [DataMember]
        public bool RepeatEffect
        {
            get;
            internal set;
        }

        [DataMember]
        public bool ShowAboutDialog
        {
            get;
            internal set;
        }

        [DataMember]
        public string SourceImagePath
        {
            get;
            internal set;
        }

        [DataMember]
        public string DestinationImagePath
        {
            get;
            internal set;
        }

        [DataMember]
        public IntPtr ParentWindowHandle
        {
            get;
            internal set;
        }

        [DataMember]
        public Color PrimaryColor
        {
            get;
            internal set;
        }

        [DataMember]
        public Color SecondaryColor
        {
            get;
            internal set;
        }

        [DataMember]
        public string RegionDataPath
        {
            get;
            internal set;
        }

        [DataMember]
        public string ParameterDataPath
        {
            get;
            internal set;
        }

        [DataMember]
        public string PseudoResourcePath
        {
            get;
            internal set;
        }

        [DataMember]
        public string DescriptorRegistryPath
        {
            get;
            internal set;
        }

        internal PSFilterShimData()
        {
        }
    }
}
