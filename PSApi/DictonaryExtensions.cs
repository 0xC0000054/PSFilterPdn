using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PSFilterLoad.PSApi
{
    public static class DictonaryExtensions
    {
        public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key, TValue value)
        {
            if (!dict.ContainsKey(key))
            {
                dict.Add(key, value);
            }
            else
            {
                dict[key] = value;
            }
        }
    }
}
