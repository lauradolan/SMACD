using Microsoft.Extensions.Logging;
using SMACD.Artifacts;
using SMACD.Artifacts.Data;
using SMACD.Artifacts.Metadata;
using Synthesys.SDK;
using Synthesys.SDK.Attributes;
using Synthesys.SDK.Capabilities;
using Synthesys.SDK.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Xml.Linq;

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
                ExecutionWrapper wrapper = new ExecutionWrapper("nmap --help");
                wrapper.Start().Wait();
                result = !wrapper.FailedToExecute;
            }
            catch (Exception)
            {
                result = false;
            }

            if (!result)
            {
                Logger.LogWarning("Nmap not installed on host");
            }

            return result;
        }

        public override ExtensionReport Act()
        {
            Logger.LogInformation("Starting Nmap plugin against host {0}", Host);

            NativeDirectoryArtifact nativePathArtifact = Host.Attachments.CreateOrLoadNativePath("nmap_" + Host.IpAddress);
            RunSingleTarget(nativePathArtifact, Host.IpAddress);

            XDocument scanXml = GetScanXml(nativePathArtifact);
            NmapRunReport nmapReport = ScoreSingleTarget(scanXml);

            ExtensionReport report = new ExtensionReport
            {
                ReportViewName = typeof(NmapReportView).FullName,
                ReportSummaryName = typeof(NmapReportSummary).FullName
            };
            report.SetExtensionSpecificReport(nmapReport);

            foreach (NmapPort port in nmapReport.Ports)
            {
                if (new[] { "httpd" }.Contains(port.Service))
                {
                    Host[$"{port.Protocol}/{port.Port}"] = new HttpServicePortArtifact();
                }

                Host[$"{port.Protocol}/{port.Port}"].Metadata.Set(
                    new ServicePortMetadata()
                    {
                        ServiceName = port.Service,
                        ProductName = port.ProductName,
                        ProductVersion = port.ProductVersion
                    },
                    "nmap",
                    DataProviderSpecificity.GeneralPurposeScanner,
                    port.ServiceFingerprintConfidence / 3.0d);

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
                    Title = $"{port.Protocol} {port.Port} open" + (port.Service == null ? "" : $" ({port.Service})")
                });
            }

            return report;
        }

        private void RunSingleTarget(NativeDirectoryArtifact artifact, string targetIp)
        {
            using (NativeDirectoryContext context = artifact.GetContext())
            {
                string cmd = $"nmap --open -T4 -PN -A {targetIp} -n -oX {context.DirectoryWithFile("scan.xml")}";

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
                XElement hostChild = xml.Root.Descendants("host").FirstOrDefault();
                if (hostChild == null)
                {
                    Logger.LogWarning("NMap report exists but does not contain any information about a remote host");
                    return result;
                }

                string addr = (string)hostChild.Descendants("address").First().Attribute("addr");
                XElement ports = hostChild.Descendants("ports").First();
                foreach (XElement portDetail in ports.Descendants("port"))
                {
                    try
                    {
                        System.Collections.Generic.IEnumerable<XAttribute> portInfo = portDetail.Attributes();
                        string protocol = (string)portDetail.Attribute("protocol");
                        string port = (string)portDetail.Attribute("portid");

                        XElement serviceDetail = portDetail.Descendants("service").First();
                        string service = (string)serviceDetail.Attribute("name");
                        string conf = (string)serviceDetail.Attribute("conf");

                        string product = (string)serviceDetail.Attribute("product");
                        string productVersion = (string)serviceDetail.Attribute("version");
                        string extraInfo = (string)serviceDetail.Attribute("extrainfo");

                        string osType = (string)serviceDetail.Attribute("ostype");

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
                        {
                            result.OperatingSystemFingerprintCandidates.Add(osType);
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogCritical(ex, "Error parsing Nmap port");
                    }
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