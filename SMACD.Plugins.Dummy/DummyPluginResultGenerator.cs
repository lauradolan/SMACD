using SMACD.Shared.Data;
using SMACD.Shared.Plugins;
using System;
using System.Threading.Tasks;

namespace SMACD.Plugins.Dummy
{
    public class DummyPluginResult : PluginResult
    {
        public int Generations { get; set; }
        public TimeSpan Duration { get; set; }

        public DummyPluginResult()
        {
        }

        public DummyPluginResult(string workingDirectory) : base(workingDirectory)
        {
        }

        public DummyPluginResult(PluginPointerModel pluginPointer, string workingDirectory) : base(pluginPointer, workingDirectory)
        {
        }

        public override async Task SummaryRunOnce(VulnerabilitySummary summary)
        {
            if (Duration != null)
                SaveResultArtifact("generations.dat", this);
            var reloaded = LoadResultArtifact<DummyPluginResult>("generations.dat");

            // This ... doesn't really do anything.
            // This is run once per module, before the method below.

            await Task.FromResult(0);
        }

        public override Task<bool> SummaryRunGenerationally(VulnerabilitySummary summary)
        {
            // Any module implementing this method will be run iteratively until all functions return "false" -- that is,
            //  all functions have *not* made a change in that generation.

            return Task.FromResult(false);
        }
    }
}