namespace PSFilterLoad.PSApi.Diagnostics
{
    internal sealed class PluginLoadingTraceListenerLogWriter : IPluginLoadingLogWriter
    {
        private PluginLoadingTraceListenerLogWriter()
        {
        }

        public static PluginLoadingTraceListenerLogWriter Instance { get; } = new PluginLoadingTraceListenerLogWriter();

        public void Write(string fileName, string message)
            => System.Diagnostics.Trace.WriteLine($"{fileName}: {message}");
    }
}
