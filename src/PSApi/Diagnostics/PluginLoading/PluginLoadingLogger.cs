using System;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace PSFilterLoad.PSApi.Diagnostics
{
    internal sealed class PluginLoadingLogger : IPluginLoadingLogger
    {
        private readonly IPluginLoadingLogWriter writer;
        private readonly ImmutableHashSet<PluginLoadingLogCategory> categories;

        public PluginLoadingLogger(IPluginLoadingLogWriter writer,
                                   ImmutableHashSet<PluginLoadingLogCategory> categories)
        {
            ArgumentNullException.ThrowIfNull(writer);
            ArgumentNullException.ThrowIfNull(categories);

            this.writer = writer;
            this.categories = categories;
        }

        public bool IsEnabled(PluginLoadingLogCategory logCategory)
        {
            return categories.Contains(logCategory);
        }

        public void Log<TValue>(string fileName,
                                PluginLoadingLogCategory logCategory,
                                TValue value,
                                OptionalParameterGuard _ = default,
                                [CallerMemberName] string memberName = "")
        {
            if (!IsEnabled(logCategory))
            {
                return;
            }

            string message = value.ToString();
            writer.Write(fileName, message);
        }

        public void Log<T0>(string fileName,
                            PluginLoadingLogCategory logCategory,
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
            writer.Write(fileName, message);
        }

        public void Log<T0, T1>(string fileName,
                                PluginLoadingLogCategory logCategory,
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
            writer.Write(fileName, message);
        }

        public void Log<T0, T1, T2>(string fileName,
                                    PluginLoadingLogCategory logCategory,
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
            writer.Write(fileName, message);
        }

        public void Log<T0, T1, T2, T3>(string fileName,
                                        PluginLoadingLogCategory logCategory,
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
            writer.Write(fileName, message);
        }

        public void Log<T0, T1, T2, T3, T4>(string fileName,
                                            PluginLoadingLogCategory logCategory,
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
            writer.Write(fileName, message);
        }

    }
}
