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

using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace PSFilterLoad.PSApi.Diagnostics
{
    internal sealed partial class PluginApiLogger : IPluginApiLogger
    {
        private readonly IPluginApiLogWriter writer;
        private readonly ImmutableHashSet<PluginApiLogCategory> categories;
        private readonly string typeName;

        private PluginApiLogger(IPluginApiLogWriter writer,
                                ImmutableHashSet<PluginApiLogCategory> categories,
                                string typeName)
        {
            ArgumentNullException.ThrowIfNull(writer);
            ArgumentNullException.ThrowIfNull(categories);
            ArgumentNullException.ThrowIfNull(typeName);

            this.writer = writer;
            this.categories = categories;
            this.typeName = typeName;
        }

        public static IPluginApiLogger Create(IPluginApiLogWriter? writer,
                                              Func<ImmutableHashSet<PluginApiLogCategory>> logCategoryFactory,
                                              string typeName)
        {
            IPluginApiLogger logger;

            if (writer is null)
            {
                logger = NullPluginApiLogger.Instance;
            }
            else
            {
                ImmutableHashSet<PluginApiLogCategory> categories = logCategoryFactory();

                logger = new PluginApiLogger(writer!, categories, typeName);
            }

            return logger;
        }

        public IPluginApiLogger CreateInstanceForType(string typeName)
            => new PluginApiLogger(writer, categories, typeName);

        public bool IsEnabled(PluginApiLogCategory logCategory)
            => categories.Contains(logCategory);

        public void Log<TValue>(PluginApiLogCategory logCategory,
                                TValue value,
                                OptionalParameterGuard _ = default,
                                [CallerMemberName] string memberName = "")
        {
            if (!IsEnabled(logCategory))
            {
                return;
            }

            string message = value?.ToString() ?? string.Empty;
            string logMessage = FormatMessage(memberName, message);
            writer.Write(logMessage);
        }

        public void Log<T0>(PluginApiLogCategory logCategory,
                            string format,
                            T0 arg0,
                            OptionalParameterGuard _ = default,
                            [CallerMemberName] string memberName = "")
        {
            if (!IsEnabled(logCategory))
            {
                return;
            }

            string message = string.Format(format, arg0);
            string logMessage = FormatMessage(memberName, message);
            writer.Write(logMessage);
        }

        public void Log<T0, T1>(PluginApiLogCategory logCategory,
                                string format,
                                T0 arg0,
                                T1 arg1,
                                OptionalParameterGuard _ = default,
                                [CallerMemberName] string memberName = "")
        {
            if (!IsEnabled(logCategory))
            {
                return;
            }

            string message = string.Format(format, arg0, arg1);
            string logMessage = FormatMessage(memberName, message);
            writer.Write(logMessage);
        }

        public void Log<T0, T1, T2>(PluginApiLogCategory logCategory,
                                    string format,
                                    T0 arg0,
                                    T1 arg1,
                                    T2 arg2,
                                    OptionalParameterGuard _ = default,
                                    [CallerMemberName] string memberName = "")
        {
            if (!IsEnabled(logCategory))
            {
                return;
            }

            string message = string.Format(format, arg0, arg1, arg2);
            string logMessage = FormatMessage(memberName, message);
            writer.Write(logMessage);
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
            if (!IsEnabled(logCategory))
            {
                return;
            }

            string message = string.Format(format, arg0, arg1, arg2, arg3);
            string logMessage = FormatMessage(memberName, message);
            writer.Write(logMessage);
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
            if (!IsEnabled(logCategory))
            {
                return;
            }

            string message = string.Format(format, arg0, arg1, arg2, arg3, arg4);
            string logMessage = FormatMessage(memberName, message);
            writer.Write(logMessage);
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
            if (!IsEnabled(logCategory))
            {
                return;
            }

            string message = string.Format(format, arg0, arg1, arg2, arg3, arg4, arg5);
            string logMessage = FormatMessage(memberName, message);
            writer.Write(logMessage);
        }

        public void LogFunctionName(PluginApiLogCategory logCategory,
                                    [CallerMemberName] string memberName = "")
        {
            if (!IsEnabled(logCategory))
            {
                return;
            }

            string logMessage = FormatFunctionName(memberName);
            writer.Write(logMessage);
        }

        private string FormatFunctionName(string functionName)
            => typeName + "." + functionName;

        private string FormatMessage(string functionName, string message)
            => FormatFunctionName(functionName) + ", " + message;
    }
}
