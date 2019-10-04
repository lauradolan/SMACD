using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using SMACD.Artifacts;
using SMACD.Artifacts.Data;
using Synthesys.SDK;
using Synthesys.SDK.Attributes;
using Synthesys.SDK.Capabilities;
using Synthesys.SDK.Extensions;

namespace Synthesys.Plugins.Nmap
{
    /// <summary>
    ///     Nmap uses raw IP packets in novel ways to determine what hosts are available on the network, what services
    ///     (application name and version) those hosts are offering, what operating systems (and OS versions) they are running,
    ///     what type of packet filters/firewalls are in use, and dozens of other characteristics.
    /// </summary>
    /// <remarks>Description from tools.kali.org</remarks>
    [Extension("nmap",
        Name = "NMap Port Scanner",
        Version = "1.0.0",
        Author = "Anthony Turner",
        Website = "https://github.com/anthturner/smacd")]
    [UseGraphicalViews(typeof(NmapReportView), typeof(NmapReportSummary))]
    public class NmapScannerAction : ActionExtension, IOperateOnHost
    {
        /// <summary>
        ///     Host/IP address to scan
        /// </summary>
        public HostArtifact Host { get; set; }

        public override bool ValidateEnvironmentReadiness()
        {
            bool result;
            try
            {
                var wrapper = new ExecutionWrapper("nmap --help");
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

            var targetIpAddress = Host.Aliases.FirstOrDefault(a => !string.IsNullOrEmpty(a));
            var nativePathArtifact = Host.Attachments.CreateOrLoadNativePath("nmap_" + Host.IpAddress);
            RunSingleTarget(nativePathArtifact, targetIpAddress);

            var scanXml = GetScanXml(nativePathArtifact);
            var nmapReport = ScoreSingleTarget(scanXml);

            var report = new ExtensionReport();
            report.ReportViewName = typeof(NmapReportView).FullName;
            report.ReportSummaryName = typeof(NmapReportSummary).FullName;
            report.SetExtensionSpecificReport(nmapReport);

            foreach (var port in nmapReport.Ports)
            {
                if (new[] {"httpd"}.Contains(port.Service))
                    Host[$"{port.Protocol}/{port.Port}"] = new HttpServicePortArtifact();

                Host[$"{port.Protocol}/{port.Port}"].ServiceName = port.Service;
                Host[$"{port.Protocol}/{port.Port}"].ProductName = port.ProductName;
                Host[$"{port.Protocol}/{port.Port}"].ProductVersion = port.ProductVersion;

                var confidenceEnum = (Vulnerability.Confidences) port.ServiceFingerprintConfidence;
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
                    Title = $"{port.Protocol} {port.Port} open" + (port.Service == null ? "" : $" ({port.Service})")
                });
            }

            return report;
        }

        private void RunSingleTarget(NativeDirectoryArtifact artifact, string targetIp)
        {
            using (var context = artifact.GetContext())
            {
                var cmd = $"nmap --open -T4 -PN -A {targetIp} -n -oX {context.DirectoryWithFile("scan.xml")}";

                var wrapper = new ExecutionWrapper(cmd);
                wrapper.StandardOutputDataReceived +=
                    (s, taskOwner, data) => Logger.TaskLogInformation(taskOwner, data);
                wrapper.StandardErrorDataReceived += (s, taskOwner, data) => Logger.TaskLogDebug(taskOwner, data);
                wrapper.Start().Wait();
            }
        }

        private XDocument GetScanXml(NativeDirectoryArtifact target)
        {
            Logger.LogDebug("Searching for scan XML output file");
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
                var hostChild = xml.Root.Descendants("host").FirstOrDefault();
                if (hostChild == null)
                {
                    Logger.LogWarning("NMap report exists but does not contain any information about a remote host");
                    return result;
                }

                var addr = (string)hostChild.Descendants("address").First().Attribute("addr");
                var ports = hostChild.Descendants("ports").First();
                foreach (var portDetail in ports.Descendants("port"))
                    try
                    {
                        var portInfo = portDetail.Attributes();
                        var protocol = (string)portDetail.Attribute("protocol");
                        var port = (string)portDetail.Attribute("portid");

                        var serviceDetail = portDetail.Descendants("service").First();
                        var service = (string)serviceDetail.Attribute("name");
                        var conf = (string)serviceDetail.Attribute("conf");

                        var product = (string)serviceDetail.Attribute("product");
                        var productVersion = (string)serviceDetail.Attribute("version");
                        var extraInfo = (string)serviceDetail.Attribute("extrainfo");

                        var osType = (string)serviceDetail.Attribute("ostype");

                        result.Ports.Add(new NmapPort
                        {
                            Protocol = Enum.Parse<ProtocolType>(protocol, true).ToString(),
                            Port = int.Parse(port),
                            Service = service,
                            ServiceFingerprintConfidence = int.Parse(conf),
                            ProductName = product,
                            ProductVersion = productVersion,
                            ExtraInfo = extraInfo
                        });

                        if (!result.OperatingSystemFingerprintCandidates.Contains(osType))
                            result.OperatingSystemFingerprintCandidates.Add(osType);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogCritical(ex, "Error parsing Nmap port");
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