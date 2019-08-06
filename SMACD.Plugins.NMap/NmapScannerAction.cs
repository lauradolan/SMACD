using Microsoft.Extensions.Logging;
using SMACD.Workspace;
using SMACD.Workspace.Actions;
using SMACD.Workspace.Artifacts;
using SMACD.Workspace.Customizations;
using SMACD.Workspace.Customizations.Correlations;
using SMACD.Workspace.Libraries;
using SMACD.Workspace.Libraries.Attributes;
using SMACD.Workspace.Targets;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Xml;
using System.Xml.Linq;

namespace SMACD.Plugins.Nmap
{
    [Implementation(ExtensionRoles.Producer, "nmap")]
    public class NmapScannerAction : ActionInstance
    {
        public NmapScannerAction() : base("NMap Scanner")
        {
        }

        public TargetDescriptor Target { get; set; }

        public override ActionSpecificReport Execute()
        {
            Logger.LogInformation("Starting Nmap plugin against target {0}", Target);
            
            var targetIpAddress = ((IHasRemoteHost)Target).RemoteHost;
            var nativePathArtifact = this.Workspace.Artifacts.CreateOrLoadNativePath("nmap_" + Target.TargetId);
            RunSingleTarget(nativePathArtifact, targetIpAddress);

            var scanXml = GetScanXml(nativePathArtifact);
            var report = ScoreSingleTarget(scanXml);

            foreach (var port in report.Ports)
            {
                Workspace.Correlations()
                    .WithHost(targetIpAddress)
                    .Ports
                    .AddPort(Enum.Parse<ProtocolType>(port.Protocol, true), port.Port)
                    .Save("service", port.Service);
            }
            foreach (var vuln in report.Vulnerabilities)
            {
                Workspace.Correlations()
                    .WithHost(targetIpAddress)
                    .Vulnerabilities
                    .AddVulnerability(vuln);
            }

            return report;
        }

        private void RunSingleTarget(NativeDirectoryArtifact artifact, string targetIp)
        {
            using (var context = artifact.GetContext())
            {
                var cmd = $"C:\\Progra~2\\Nmap\\nmap.exe ";
                var cmd2 = $" --open -T4 -PN {targetIp} -n -oX {context.DirectoryWithFile("scan.xml")}";

                var wrapper = new ExecutionWrapper(cmd+cmd2);
                wrapper.StandardOutputDataReceived +=
                    (s, taskOwner, data) => Logger.TaskLogInformation(taskOwner, data);
                wrapper.StandardErrorDataReceived += (s, taskOwner, data) => Logger.TaskLogDebug(taskOwner, data);
                wrapper.Start().Wait();
            }
        }

        private XDocument GetScanXml(NativeDirectoryArtifact target)
        {
            Logger.LogDebug("Searching for scan XML output file");
            XDocument xml = null;
            using (var context = target.GetContext())
            {
                var scanFile = context.DirectoryWithFile("scan.xml");
                if (!File.Exists(scanFile))
                {
                    Logger.LogCritical("XML report from this plugin was not found! Aborting...");
                    return null;
                }
                return XDocument.Load(scanFile);
            }
        }

        private NmapRunReport ScoreSingleTarget(XDocument xml)
        {
            var result = new NmapRunReport();
            try
            {
                var start = xml.Root.Attributes("start").First().Value;
                result.TimeOfExecution = DateTime.FromFileTime(long.Parse(start));

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

                    result.Ports.Add(new NmapPort
                    {
                        Protocol = protocol,
                        Port = int.Parse(port),
                        Service = service,
                        ServiceFingerprintConfidence = int.Parse(conf)
                    });

                    var confidenceEnum = (Vulnerability.Confidences)(2.0 / 5.0 * int.Parse(conf));
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
                        Target = new RawPortTarget
                        {
                            RemoteHost = addr,
                            Port = int.Parse(port),
                            Protocol = Enum.Parse<ProtocolType>(protocol, true)
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Error working with Nmap XML output!");
            }
            return result;
        }
    }
}