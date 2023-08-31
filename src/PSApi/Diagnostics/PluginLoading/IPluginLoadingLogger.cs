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

using System.Runtime.CompilerServices;

namespace PSFilterLoad.PSApi.Diagnostics
{
    internal interface IPluginLoadingLogger
    {
        bool IsEnabled(PluginLoadingLogCategory logCategory);

        void Log<TValue>(string fileName,
                         PluginLoadingLogCategory logCategory,
                         TValue value,
                         OptionalParameterGuard _ = default,
                         [CallerMemberName] string memberName = "");

        void Log<T0>(string fileName,
                     PluginLoadingLogCategory logCategory,
                     string format,
                     T0 arg0,
                     OptionalParameterGuard _ = default,
                     [CallerMemberName] string memberName = "");

        void Log<T0, T1>(string fileName,
                         PluginLoadingLogCategory logCategory,
                         string format,
                         T0 arg0,
                         T1 arg1,
                         OptionalParameterGuard _ = default,
                         [CallerMemberName] string memberName = "");

        void Log<T0, T1, T2>(string fileName,
                             PluginLoadingLogCategory logCategory,
                             string format,
                             T0 arg0,
                             T1 arg1,
                             T2 arg2,
                             OptionalParameterGuard _ = default,
                             [CallerMemberName] string memberName = "");

        void Log<T0, T1, T2, T3>(string fileName,
                                 PluginLoadingLogCategory logCategory,
                                 string format,
                                 T0 arg0,
                                 T1 arg1,
                                 T2 arg2,
                                 T3 arg3,
                                 OptionalParameterGuard _ = default,
                                 [CallerMemberName] string memberName = "");

        void Log<T0, T1, T2, T3, T4>(string fileName,
                                     PluginLoadingLogCategory logCategory,
                                     string format,
                                     T0 arg0,
                                     T1 arg1,
                                     T2 arg2,
                                     T3 arg3,
                                     T4 arg4,
                                     OptionalParameterGuard _ = default,
                                     [CallerMemberName] string memberName = "");
    }
}
