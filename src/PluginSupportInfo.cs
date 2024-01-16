/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2024 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Reflection;
using PaintDotNet;

namespace PSFilterPdn
{
    public sealed class PluginSupportInfo : IPluginSupportInfo
    {
        public string Author
        {
            get
            {
                return "null54";
            }
        }

        public string Copyright
        {
            get
            {
                return ((AssemblyCopyrightAttribute)typeof(PSFilterPdnEffect).Assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false)[0]).Copyright;
            }
        }

        public string DisplayName
        {
            get
            {
                return PSFilterPdnEffect.StaticName;
            }
        }

        public Version Version
        {
            get
            {
                return typeof(PSFilterPdnEffect).Assembly.GetName().Version!;
            }
        }

        public Uri WebsiteUri
        {
            get
            {
                return new Uri("http://forums.getpaint.net/index.php?showtopic=20622");
            }
        }
    }
}
