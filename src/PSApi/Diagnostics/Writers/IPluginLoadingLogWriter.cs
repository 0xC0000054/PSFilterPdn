using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PSFilterLoad.PSApi.Diagnostics
{
    internal interface IPluginLoadingLogWriter
    {
        void Write(string filename, string message);
    }
}
