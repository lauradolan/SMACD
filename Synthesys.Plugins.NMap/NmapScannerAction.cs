using Microsoft.Extensions.Logging;
using SMACD.Artifacts;
using SMACD.Artifacts.Data;
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Xml.Linq;
using Synthesys.SDK;
using Synthesys.SDK.Attributes;
using Synthesys.SDK.Capabilities;
using Synthesys.SDK.Extensions;

namespace Synthesys.Plugins.Nmap
{
    /// <summary>
    /// Nmap uses raw IP packets in novel ways to determine what hosts are available on the network, what services (application name and version) those hosts are offering, what operating systems (and OS versions) they are running, what type of packet filters/firewalls are in use, and dozens of other characteristics.
    /// </summary>
    /// <remarks>Description from tools.kali.org</remarks>
    [Extension("nmap",
        Name = "NMap Port Scanner",
        Version = "1.0.0",
        Author = "Anthony Turner",
        Website = "https://github.com/anthturner/smacd")]
    public class NmapScannerAction : ActionExtension, IOperateOnHost
    {
        /// <summary>
        /// Host/IP address to scan
        /// </summary>
        public HostArtifact Host { get; set; }

        public override bool ValidateEnvironmentReadiness()
        {
            bool result;
            try
            {
                ExecutionWrapper wrapper = new ExecutionWrapper("nmap --help");
                wrapper.Start().Wait();
                result = !wrapper.FailedToExecute;
            }
            catch (Exception ex)
            {
                result = false;
            }
            if (!result)
                Logger.LogWarning("Nmap not installed on host");
            return result;
        }

        public override ExtensionReport Act()
        {
            Logger.LogInformation("Starting Nmap plugin against host {0}", Host);

            string targetIpAddress = Host.IpAddress;
            var nativePathArtifact = Host.Attachments.CreateOrLoadNativePath("nmap_" + Host.IpAddress);
            RunSingleTarget(nativePathArtifact, targetIpAddress);

            XDocument scanXml = GetScanXml(nativePathArtifact);
            NmapRunReport report = ScoreSingleTarget(scanXml);

            foreach (NmapPort port in report.Ports)
            {
                if (new string[] { "httpd" }.Contains(port.Service))
                {
                    Host[$"{port.Protocol}/{port.Port}"] = new HttpServicePortArtifact();
                }

                Host[$"{port.Protocol}/{port.Port}"].ServiceName = port.Service;

                Vulnerability.Confidences confidenceEnum = (Vulnerability.Confidences)port.ServiceFingerprintConfidence;
                Host[$"{port.Protocol}/{port.Port}"].Vulnerabilities.Add(new Vulnerability
                {
                    Confidence = confidenceEnum,
                    RiskLevel = Vulnerability.RiskLevels.Informational,
                    Description =
                        $"NMap found an open port {port.Protocol} {port.Port} on {Host.Hostname}. NMap's guess for this service is {port.Service} (confidence: {port.ServiceFingerprintConfidence} - {confidenceEnum})",
                    Occurrences = 1,
                    Remedy =
                        "If this port should be open to provide a service, there is no need for a change. Otherwise, find out if this port needs to be opened, and if not, " +
                        "terminate the service using it, or apply firewall rules to prevent its access from the open Internet.",
                    Title = $"{port.Protocol} {port.Port} open" + (port.Service == null ? "" : $" ({port.Service})"),
                });
            }

            return report;
        }

        private void RunSingleTarget(NativeDirectoryArtifact artifact, string targetIp)
        {
            using (NativeDirectoryContext context = artifact.GetContext())
            {
                string cmd = $"nmap --open -T4 -PN {targetIp} -n -oX {context.DirectoryWithFile("scan.xml")}";

                ExecutionWrapper wrapper = new ExecutionWrapper(cmd);
                wrapper.StandardOutputDataReceived +=
                    (s, taskOwner, data) => Logger.TaskLogInformation(taskOwner, data);
                wrapper.StandardErrorDataReceived += (s, taskOwner, data) => Logger.TaskLogDebug(taskOwner, data);
                wrapper.Start().Wait();
            }
        }

        private XDocument GetScanXml(NativeDirectoryArtifact target)
        {
            Logger.LogDebug("Searching for scan XML output file");
            using (NativeDirectoryContext context = target.GetContext())
            {
                string scanFile = context.DirectoryWithFile("scan.xml");
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
            NmapRunReport result = new NmapRunReport();
            try
            {
                XElement addrChild = xml.Root.Descendants("address").FirstOrDefault();
                if (addrChild == null)
                {
                    Logger.LogWarning("NMap report exists but does not contain any information about a remote host");
                    return result;
                }

                string addr = xml.Root.Descendants("address").First().Attributes("addr").First().Value;
                XElement ports = xml.Root.Descendants("ports").First();
                foreach (XElement portDetail in ports.Descendants("port"))
                {
                    try
                    {
                        System.Collections.Generic.IEnumerable<XAttribute> portInfo = portDetail.Attributes();
                        string protocol = portDetail.Attributes("protocol").First().Value;
                        string port = portDetail.Attributes("portid").First().Value;

                        XElement serviceDetail = portDetail.Descendants("service").First();
                        string service = serviceDetail.Attributes("name").First().Value;
                        string conf = serviceDetail.Attributes("conf").First().Value;

                        result.Ports.Add(new NmapPort
                        {
                            Protocol = Enum.Parse<ProtocolType>(protocol, true).ToString(),
                            Port = int.Parse(port),
                            Service = service,
                            ServiceFingerprintConfidence = int.Parse(conf)
                        });
                    } catch (Exception ex) { Logger.LogCritical(ex, "Error parsing Nmap port"); }
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