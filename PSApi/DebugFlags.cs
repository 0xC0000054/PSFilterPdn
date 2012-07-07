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
        ChannelPorts = 8,
        ColorServices = 16,
        DescriptorParameters = 32,
        DisplayPixels = 64,
        Error = 128,
        HandleSuite = 256,
        MiscCallbacks = 512,
        PiPL = 1024
    }
#endif
}
