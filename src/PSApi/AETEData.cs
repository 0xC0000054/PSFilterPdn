/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2022 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;

namespace PSFilterLoad.PSApi
{
    [System.Serializable()]
    internal sealed class AETEData
    {
        private Dictionary<uint, short> flagList;

        internal AETEData(Dictionary<uint, short> aeteParameterFlags)
        {
            flagList = new Dictionary<uint, short>(aeteParameterFlags);
        }

        public bool TryGetParameterFlags(uint key, out short flags)
        {
            return flagList.TryGetValue(key, out flags);
        }
    }
}
