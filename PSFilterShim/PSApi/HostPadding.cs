using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PSFilterLoad.PSApi
{   
    /// <summary>
    /// The padding values used by the FilterRecord inputPadding and maskPadding.
    /// </summary>
    internal static class HostPadding
    {
        public const short plugInWantsEdgeReplication = -1;
        public const short plugInDoesNotWantPadding = -2;
        public const short plugInWantsErrorOnBoundsException = -3;
    }
}
