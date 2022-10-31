namespace PSFilterLoad.PSApi.Diagnostics
{
    internal interface IPluginLoadingLogWriter
    {
        void Write(string filename, string message);
    }
}
