using System.Collections.Generic;
using System.Linq;
using SMACD.PluginHost.Extensions;
using SMACD.PluginHost.Plugins;

namespace SMACD.PluginHost
{
    public class WorkingDirectoryLayout
    {
        public Dictionary<string, string> Options { get; set; } = new Dictionary<string, string>();
    }

    public class ResourceWorkingDirectoryLayout : WorkingDirectoryLayout
    {
        public List<PluginSummary> Plugins { get; set; } = new List<PluginSummary>();
        // TODO: Internal the List<T> and write methods to GetLastX() and Count

        public PluginSummary GetLast(string identifier) => Plugins.FindLast(p => p.Identifier == identifier);
        public PluginSummary GetLast(PluginTypes type) => Plugins.FindLast(p => PluginLibrary.PluginsAvailable[p.Identifier].PluginType == type);
        public PluginSummary GetLast() => Plugins.Last();
    }

    public class PluginWorkingDirectoryLayout : WorkingDirectoryLayout
    {
    }
}