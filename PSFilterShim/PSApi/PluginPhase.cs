using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PSFilterLoad.PSApi
{
#if DEBUG
    enum PluginPhase
    {
        None,
        Parameters,
        Prepare,
        Start,
        Finish
    } 
#endif
}
