using System.Collections.Immutable;

namespace PSFilterLoad.PSApi.Diagnostics
{
    internal static class PluginLoadingLogCategories
    {
        public static ImmutableHashSet<PluginLoadingLogCategory> Default = BuildDefaultCategories();

        private static ImmutableHashSet<PluginLoadingLogCategory> BuildDefaultCategories()
        {
#if DEBUG
            return ImmutableHashSet.Create(PluginLoadingLogCategory.Error, PluginLoadingLogCategory.Warning);
#else
            return ImmutableHashSet.Create(PluginLoadingLogCategory.Error);
#endif
        }
    }
}
