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

namespace PSFilterPdn
{
    [DataContract]
    internal sealed class PSFilterShimSettings
    {
        internal PSFilterShimSettings()
        {
        }

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
        public int PrimaryColor
        {
            get;
            internal set;
        }

        [DataMember]
        public int SecondaryColor
        {
            get;
            internal set;
        }

        [DataMember]
        public string SelectionMaskPath
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

        [DataMember]
        public string LogFilePath
        {
            get;
            internal set;
        }

        [DataMember]
        public PSFilterLoad.PSApi.PluginUISettings PluginUISettings
        {
            get;
            internal set;
        }
    }
}
