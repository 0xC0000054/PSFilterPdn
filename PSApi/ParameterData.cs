using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PSFilterLoad.PSApi
{
    /// <summary>
    /// The struct that holds the saved filter global parameter data.
    /// </summary>
    [Serializable]
    public sealed class ParameterData 
    {
        private GlobalParameters globalParms;
        private Dictionary<uint, AETEValue> aeteDict;

        public GlobalParameters GlobalParms
        {
            get 
            {
                return globalParms;
            }
        }

        public Dictionary<uint, AETEValue> AETEDict
        {
            get
            {
                return aeteDict;
            }
        }

        public ParameterData(GlobalParameters globals, Dictionary<uint, AETEValue> aete)
        {
            this.globalParms = globals;

            if (aete != null)
            {
                this.aeteDict = new Dictionary<uint, AETEValue>(aete);
            }
            else
            {
                this.aeteDict = null;
            }
        }
       
    }
}
