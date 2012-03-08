using System;

namespace PSFilterLoad.PSApi
{
#if DEBUG
    [Flags]
    enum DebugFlags
    {
        None = 0,
        AdvanceState = 1,
        BufferSuite = 2,
        Call = 4,
        ColorServices = 8,
        DescriptorParameters = 16,
        DisplayPixels = 32,
        Error = 64,
        HandleSuite = 128,
        MiscCallbacks = 256,
        PiPL = 512
    }
#endif
}
