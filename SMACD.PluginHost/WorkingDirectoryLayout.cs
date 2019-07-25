using SMACD.PluginHost.Plugins;
using System.Collections.Generic;

namespace SMACD.PluginHost
{
    public class WorkingDirectoryLayout
    {
        public Dictionary<string, string> Options { get; set; }
    }

    public class ResourceWorkingDirectoryLayout : WorkingDirectoryLayout
    {
        public List<PluginSummary> Plugins { get; set; }
    }

    public class PluginWorkingDirectoryLayout : WorkingDirectoryLayout { }
}
