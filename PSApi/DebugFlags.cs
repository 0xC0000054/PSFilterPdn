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
        DescriptorParameters = 10,
        DisplayPixels = 12,
        Error = 14,
        HandleSuite = 16,
        MiscCallbacks = 18,
        PiPL = 20,
    }
#endif
}
