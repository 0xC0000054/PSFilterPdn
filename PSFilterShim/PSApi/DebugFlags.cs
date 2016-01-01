/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2016 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace PSFilterLoad.PSApi
{
#if DEBUG
    [Flags]
    internal enum DebugFlags
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
        ResourceSuite = (1 << 13),
        SPBasicSuite = (1 << 14)
    }
#endif
}
