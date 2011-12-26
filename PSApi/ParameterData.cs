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
        private GlobalParameters globalParameters;
        private Dictionary<uint, AETEValue> aeteDict;

        public GlobalParameters GlobalParameters
        {
            get 
            {
                return globalParameters;
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
            this.globalParameters = globals;

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
