using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using SMACD.PluginHost.Attributes;
using SMACD.PluginHost.Extensions;
using SMACD.PluginHost.Plugins;
using SMACD.PluginHost.Reports;
using SMACD.PluginHost.Resources;

namespace SMACD.Plugins.Nmap
{
    [PluginImplementation(PluginTypes.Scorer, "nmap")]
    public class NmapScannerScorer : Plugin
    {
        public NmapScannerScorer(string workingDirectory) : base(workingDirectory)
        {
        }

        public override ScoredResult Execute()
        {
            Logger.LogDebug("Starting Nmap scorer");
            var result = CreateBlankScoredResult();
            var runObject = new NmapRun();
            var scanFile = WorkingDirectory.ParentResource.GetMostRecent(PluginTypes.AttackTool).WithFile("scan.xml");
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

                        runObject.Ports.Add(new NmapPort
                        {
                            Protocol = protocol,
                            Port = int.Parse(port),
                            Service = service,
                            ServiceFingerprintConfidence = int.Parse(conf)
                        });

                        var confidenceEnum = (Vulnerability.Confidences) (2.0 / 5.0 * int.Parse(conf));
                        ((IList)result.Vulnerabilities).Add(new Vulnerability
                        {
                            Confidence = confidenceEnum,
                            RiskLevel = Vulnerability.RiskLevels.Informational,
                            Description =
                                $"NMap found an open port {protocol} {port} on {addr}. NMap's guess for this service is {service} (confidence: {conf} - {confidenceEnum})",
                            Occurrences = 1,
                            Remedy =
                                "If this port should be open to provide a service, there is no need for a change. Otherwise, find out if this port needs to be opened, and if not, " +
                                "terminate the service using it, or apply firewall rules to prevent its access from the open Internet.",
                            ShortName = $"{protocol} {port} open" + (service == null ? "" : $" ({service})"),
                            Target = new SocketPortResource
                            {
                                Hostname = addr,
                                Port = int.Parse(port),
                                Protocol = protocol,
                                ServiceGuess = service
                            }
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
                return CreateBlankScoredResult();
            }

            return result;

            // TODO: Migrate HTML report into AzDO plugin?
        }
    }
}