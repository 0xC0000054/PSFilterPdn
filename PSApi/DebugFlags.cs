using System;

namespace PSFilterLoad.PSApi
{
#if DEBUG
    [Flags]
    enum DebugFlags
    {
        None = 0,
        AdvanceState = 2,
        BufferSuite = 4,
        Call = 6,
        ColorServices = 8,
        DisplayPixels = 10,
        Error = 12,
        HandleSuite = 14,
        MiscCallbacks = 16,
        PiPL = 18,

    }
#endif
}
