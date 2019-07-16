using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using SMACD.Plugins.Nmap;
using SMACD.ScannerEngine;
using SMACD.ScannerEngine.Attributes;
using SMACD.ScannerEngine.Plugins;
using SMACD.ScannerEngine.Resources;

namespace SMACD.Plugins.OwaspZap
{
    [PluginMetadata("nmap", Name = "Nmap Port Scanner Default Scorer")]
    public class NmapScannerScorer : ScorerPlugin
    {

        public override async Task Score(VulnerabilitySummary summary)
        {
            var runObject = new NmapRun();
            var scanFile = Path.Combine(WorkingDirectory, "scan.xml");
            scanFile = "C:\\Working Folder\\GitHub\\SMACD\\scan.xml";
            if (File.Exists(scanFile))
            {
                try
                {
                    var xml = XDocument.Load(scanFile);
                    var start = xml.Root.Attributes("start").First().Value;
                    runObject.RunTime = DateTime.FromFileTime(long.Parse(start));

                    var addr = xml.Root.Descendants("address").First().Attributes("addr").First().Value;
                    var ports = xml.Root.Descendants("ports").First();
                    foreach (var portDetail in ports.Descendants("port"))
                    {
                        var portInfo = portDetail.Attributes();
                        var protocol = portDetail.Attributes("protocol").First().Value;
                        var port = portDetail.Attributes("portid").First().Value;

                        var serviceDetail = portDetail.Descendants("service").First();
                        var service = serviceDetail.Attributes("name").First().Value;
                        var conf = serviceDetail.Attributes("conf").First().Value;

                        runObject.Ports.Add(new NmapPort()
                        {
                            Protocol = protocol,
                            Port = Int32.Parse(port),
                            Service = service,
                            ServiceFingerprintConfidence = Int32.Parse(conf)
                        });
                        
                        summary.DiscoveredResources.Add(new SocketPortResource()
                        {
                            Hostname = addr,
                            Protocol = protocol,
                            Port = Int32.Parse(port),
                            ServiceGuess = service,
                            SystemCreated = true
                        });
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogCritical(ex, "Error deserializing Nmap XML output report!");
                }
            }
            else
            {
                Logger.LogCritical("XML report from this plugin was not found! Aborting...");
                return;
            }

            // TODO: Migrate HTML report into AzDO plugin?
        }

        public override async Task<bool> Converge(VulnerabilitySummary summary)
        {
            // Nothing to converge!
            return false;
        }
    }
}