using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PSFilterLoad.PSApi
{
    public static class DictonaryExtensions
    {
        public static void AddOrUpdate<TKey, TValue>(this IDictionary<TKey, TValue> dictonary, TKey key, TValue value)
        {
            if (!dictonary.ContainsKey(key))
            {
                dictonary.Add(key, value);
            }
            else
            {
                dictonary[key] = value;
            }
        }
    }
}
