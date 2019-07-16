using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SMACD.ScannerEngine.Attributes;
using SMACD.ScannerEngine.Extensions;
using SMACD.ScannerEngine.Plugins;
using SMACD.ScannerEngine.Resources;

namespace SMACD.Plugins.Nmap
{
    [AllowResourceType(typeof(HttpResource))]
    [PluginMetadata("nmap", Name = "Nmap Port Scanner", DefaultScorer = "nmap")]
    public class NmapAttackTool : AttackToolPlugin
    {
        public override async Task Execute()
        {
            Logger.LogInformation("Starting Nmap plugin against Resources {0}", string.Join(", ", Resources));
            foreach (var resource in Resources)
            {
                string subnet = string.Empty;
                if (resource is HttpResource)
                {
                    var uri = ((HttpResource) resource).UriInstance;
                    subnet = uri.DnsSafeHost;
                }
                var cmd = $"nmap --open -T4 -PN {subnet} -n -oX {Path.Combine(WorkingDirectory, "scan.xml")}";

                var wrapper = new ExecutionWrapper(cmd);
                wrapper.StandardOutputDataReceived += (s, taskOwner, data) => Logger.TaskLogInformation(taskOwner, data);
                wrapper.StandardErrorDataReceived += (s, taskOwner, data) => Logger.TaskLogDebug(taskOwner, data);
                wrapper.Start(Pointer).Wait();
            }
        }
    }
}
