using Microsoft.Extensions.Logging;
using SMACD.PluginHost.Attributes;
using SMACD.PluginHost.Extensions;
using SMACD.PluginHost.Plugins;
using SMACD.PluginHost.Reports;
using SMACD.PluginHost.Resources;

namespace SMACD.Plugins.Nmap
{
    [PluginImplementation(PluginTypes.AttackTool, "nmap")]
    public class NmapAttackTool : Plugin
    {
        public NmapAttackTool(string workingDirectory) : base(workingDirectory)
        {
        }

        public HttpResource WebEndpointResource { get; set; }
        public SocketPortResource SocketEndpointResource { get; set; }

        public override ScoredResult Execute()
        {
            Logger.LogInformation("Starting Nmap plugin against Resources {0}", string.Join(", ", Resources));
            foreach (var resource in Resources)
            {
                var subnet = string.Empty;
                if (resource is HttpResource)
                {
                    var uri = ((HttpResource)resource).UriInstance;
                    subnet = uri.DnsSafeHost;
                }
                else if (resource is SocketPortResource)
                {
                    subnet = ((SocketPortResource)resource).Hostname;
                }

                var cmd = $"nmap --open -T4 -PN {subnet} -n -oX {WorkingDirectory.WithFile("scan.xml")}";

                var wrapper = new ExecutionWrapper(cmd);
                wrapper.StandardOutputDataReceived +=
                    (s, taskOwner, data) => Logger.TaskLogInformation(taskOwner, data);
                wrapper.StandardErrorDataReceived += (s, taskOwner, data) => Logger.TaskLogDebug(taskOwner, data);
                wrapper.Start().Wait();
            }

            return GetScoredResult();
        }
    }
}