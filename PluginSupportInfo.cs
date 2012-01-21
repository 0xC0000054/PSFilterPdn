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
                return ((AssemblyCopyrightAttribute)base.GetType().Assembly.GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false)[0]).Copyright; 
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
                return base.GetType().Assembly.GetName().Version;
            }
        }

        public Uri WebsiteUri
        {
            get 
            {
                return new Uri("http://www.getpaint.net/redirect/plugins.html");
            }
        }
    }
}
