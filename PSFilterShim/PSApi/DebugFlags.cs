using System;

namespace PSFilterLoad.PSApi
{
#if DEBUG
    [Flags]
    enum DebugFlags
    {
        None = 0,
        AdvanceState = (1 << 0),
        BufferSuite = (1 << 1),
        Call = (1 << 2),
        ChannelPorts = (1 << 3),
        ColorServices = (1 << 4),
        DescriptorParameters = (1 << 5),
        DisplayPixels = (1 << 6),
        Error = (1 << 7),
        HandleSuite = (1 << 8),
        ImageServices = (1 << 9),
        MiscCallbacks = (1 << 10),
        PiPL = (1 << 11),
        PropertySuite = (1 << 12),
        ResourceSuite = (1 << 13)
    }
#endif
}
