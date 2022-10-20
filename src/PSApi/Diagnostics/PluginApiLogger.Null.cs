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

using System.Runtime.CompilerServices;

#nullable enable

namespace PSFilterLoad.PSApi.Diagnostics
{
    internal sealed partial class PluginApiLogger
    {
        private sealed class NullPluginApiLogger : IPluginApiLogger
        {
            private NullPluginApiLogger()
            {
            }

            public static NullPluginApiLogger Instance { get; } = new NullPluginApiLogger();

            public IPluginApiLogger CreateInstanceForType(string typeName) => Instance;

            public bool IsEnabled(PluginApiLogCategory logCategory) => false;

            public void Log<TValue>(PluginApiLogCategory logCategory,
                                    TValue value,
                                    OptionalParameterGuard _ = default,
                                    [CallerMemberName] string memberName = "")
            {
            }

            public void Log<T0>(PluginApiLogCategory logCategory,
                                string format,
                                T0 arg0,
                                OptionalParameterGuard _ = default,
                                [CallerMemberName] string memberName = "")
            {
            }

            public void Log<T0, T1>(PluginApiLogCategory logCategory,
                                    string format,
                                    T0 arg0,
                                    T1 arg1,
                                    OptionalParameterGuard _ = default,
                                    [CallerMemberName] string memberName = "")
            {
            }

            public void Log<T0, T1, T2>(PluginApiLogCategory logCategory,
                                        string format,
                                        T0 arg0,
                                        T1 arg1,
                                        T2 arg2,
                                        OptionalParameterGuard _ = default,
                                        [CallerMemberName] string memberName = "")
            {
            }

            public void Log<T0, T1, T2, T3>(PluginApiLogCategory logCategory,
                                            string format,
                                            T0 arg0,
                                            T1 arg1,
                                            T2 arg2,
                                            T3 arg3,
                                            OptionalParameterGuard _ = default,
                                            [CallerMemberName] string memberName = "")
            {
            }

            public void Log<T0, T1, T2, T3, T4>(PluginApiLogCategory logCategory,
                                                string format,
                                                T0 arg0,
                                                T1 arg1,
                                                T2 arg2,
                                                T3 arg3,
                                                T4 arg4,
                                                OptionalParameterGuard _ = default,
                                                [CallerMemberName] string memberName = "")
            {
            }

            public void Log<T0, T1, T2, T3, T4, T5>(PluginApiLogCategory logCategory,
                                                    string format,
                                                    T0 arg0,
                                                    T1 arg1,
                                                    T2 arg2,
                                                    T3 arg3,
                                                    T4 arg4,
                                                    T5 arg5,
                                                    OptionalParameterGuard _ = default,
                                                    [CallerMemberName] string memberName = "")
            {
            }

            public void LogFunctionName(PluginApiLogCategory logCategory, [CallerMemberName] string memberName = "")
            {
            }
        }
    }
}
