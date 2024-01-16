/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2024 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System.Runtime.CompilerServices;

namespace PSFilterLoad.PSApi.Diagnostics
{
    internal interface IPluginApiLogger
    {
        IPluginApiLogger CreateInstanceForType(string typeName);

        bool IsEnabled(PluginApiLogCategory logCategory);

        void LogFunctionName(PluginApiLogCategory logCategory,
                             [CallerMemberName] string memberName = "");

        void Log<TValue>(PluginApiLogCategory logCategory,
                         TValue value,
                         OptionalParameterGuard _ = default,
                         [CallerMemberName] string memberName = "");

        void Log<T0>(PluginApiLogCategory logCategory,
                     string format,
                     T0 arg0,
                     OptionalParameterGuard _ = default,
                     [CallerMemberName] string memberName = "");

        void Log<T0, T1>(PluginApiLogCategory logCategory,
                         string format,
                         T0 arg0,
                         T1 arg1,
                         OptionalParameterGuard _ = default,
                         [CallerMemberName] string memberName = "");

        void Log<T0, T1, T2>(PluginApiLogCategory logCategory,
                             string format,
                             T0 arg0,
                             T1 arg1,
                             T2 arg2,
                             OptionalParameterGuard _ = default,
                             [CallerMemberName] string memberName = "");

        void Log<T0, T1, T2, T3>(PluginApiLogCategory logCategory,
                                 string format,
                                 T0 arg0,
                                 T1 arg1,
                                 T2 arg2,
                                 T3 arg3,
                                 OptionalParameterGuard _ = default,
                                 [CallerMemberName] string memberName = "");

        void Log<T0, T1, T2, T3, T4>(PluginApiLogCategory logCategory,
                                     string format,
                                     T0 arg0,
                                     T1 arg1,
                                     T2 arg2,
                                     T3 arg3,
                                     T4 arg4,
                                     OptionalParameterGuard _ = default,
                                     [CallerMemberName] string memberName = "");

        void Log<T0, T1, T2, T3, T4, T5>(PluginApiLogCategory logCategory,
                                         string format,
                                         T0 arg0,
                                         T1 arg1,
                                         T2 arg2,
                                         T3 arg3,
                                         T4 arg4,
                                         T5 arg5,
                                         OptionalParameterGuard _ = default,
                                         [CallerMemberName] string memberName = "");
    }
}
