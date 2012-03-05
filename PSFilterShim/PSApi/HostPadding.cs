using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PSFilterLoad.PSApi
{
    /// <summary>
    /// The padding values used by the FilterRecord inputPadding and maskPadding.
    /// </summary>
    internal enum HostPadding : short
    {
        plugInWantsEdgeReplication = -1,
        plugInDoesNotWantPadding = -2,
        plugInWantsErrorOnBoundsException = -3
    }
}
