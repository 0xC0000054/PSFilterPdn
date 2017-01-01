/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2017 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;

namespace PSFilterLoad.PSApi
{
    [System.Serializable()]
    public sealed class AETEData
    {
        private Dictionary<uint, short> flagList;

        public IEnumerable<KeyValuePair<uint, short>> FlagList
        {
            get
            {
                return flagList;
            }
        }

        internal AETEData(PluginAETE enumAETE)
        {
            this.flagList = new Dictionary<uint, short>();
            
            foreach (AETEParameter parm in enumAETE.scriptEvent.parameters)
            {
                if (!flagList.ContainsKey(parm.key))
                {
                    flagList.Add(parm.key, parm.flags);
                }
            }
            
        }
    } 
    

}
