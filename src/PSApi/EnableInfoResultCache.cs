/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2023 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterPdn.EnableInfo;
using System;
using System.Collections.Generic;

namespace PSFilterLoad.PSApi
{
    internal sealed class EnableInfoResultCache
    {
        private readonly Dictionary<string, EnableInfoData> values;

        private EnableInfoResultCache()
        {
            values = new Dictionary<string, EnableInfoData>(StringComparer.OrdinalIgnoreCase);
        }

        public static EnableInfoResultCache Instance { get; } = new EnableInfoResultCache();

        public bool? TryGetValue(string enableInfo, EnableInfoVariables variables)
        {
            bool? result = null;

            try
            {
                if (values.TryGetValue(enableInfo, out EnableInfoData data))
                {
                    result = data.TryGetResult(variables);
                }
                else
                {
                    data = new EnableInfoData(enableInfo);

                    result = data.TryGetResult(variables);

                    values.Add(enableInfo, data);
                }
            }
            catch (EnableInfoException)
            {
                // Ignore any errors that occur when evaluating the enable info expression.
            }

            return result;
        }

        private sealed class EnableInfoData
        {
            private readonly Expression expression;
            private readonly Dictionary<EnableInfoVariables, bool> resultCache;

            internal EnableInfoData(string enableInfo)
            {
                try
                {
                    expression = EnableInfoParser.Parse(enableInfo);
                }
                catch (EnableInfoException)
                {
                    expression = null;
                }
                resultCache = new Dictionary<EnableInfoVariables, bool>();
            }

            internal bool? TryGetResult(EnableInfoVariables variables)
            {
                if (!resultCache.TryGetValue(variables, out bool result))
                {
                    if (expression != null)
                    {
                        result = new EnableInfoInterpreter(variables).Evaluate(expression);

                        resultCache.Add(variables, result);
                    }
                    else
                    {
                        return null;
                    }
                }

                return result;
            }
        }
    }
}
