﻿using Microsoft.Extensions.Logging;
using SMACD.AppTree;
using SMACD.AppTree.Details;
using SMACD.AppTree.Evidence;
using Synthesys.SDK;
using Synthesys.SDK.Attributes;
using Synthesys.SDK.Capabilities;
using Synthesys.SDK.Extensions;
using Synthesys.SDK.HostCommands;
using System;
using System.Collections.Generic;
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
        public HostNode Host { get; set; }

        private Dictionary<string, int> _portPointValues = new Dictionary<string, int>()
        {
            { "httpd", 5 },
            { "pop3d", 5 },
            { "imapd", 5 },
            { "smtp", 5 },
            { "nameserver", 10 },
            { "tftp", 10 },
            { "kerberos-sec", 10 },
            { "netbios-ssn", 10 },
            { "netbios-ns", 10 },
            { "snmp", 10 },
            { "sftp", 5 },
            { "ftp", 5 },
            { "ftp-data", 5 },
            { "ssh", 15 },
            { "telnet", 20 },
            { "rdp", 15 }
        };

        public override ExtensionReport Act()
        {
            Logger.LogInformation("Starting Nmap plugin against host {0}", Host);

            NativeDirectoryEvidence nativePathArtifact = Host.Evidence.CreateOrLoadNativePath("nmap_" + Host.IpAddress);
            RunSingleTarget(nativePathArtifact, Host.IpAddress);

            XDocument scanXml = GetScanXml(nativePathArtifact);
            NmapRunReport nmapReport = ScoreSingleTarget(scanXml);

            ExtensionReport report = new ExtensionReport
            {
                ReportViewName = typeof(NmapReportView).FullName,
                ReportSummaryName = typeof(NmapReportSummary).FullName
            };

            if (!Host.Root.LockTreeNodes)
            {
                foreach (NmapPort port in nmapReport.Ports)
                {
                    if (new[] { "httpd" }.Contains(port.Service))
                    {
                        Host[$"{port.Protocol}/{port.Port}"] = new HttpServiceNode(Host, $"{port.Protocol}/{port.Port}");
                    }

                    Host[$"{port.Protocol}/{port.Port}"].Detail.Set(
                        new ServiceDetails()
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
            }

            var totalPoints = 100;
            foreach (var port in nmapReport.Ports)
            {
                if (_portPointValues.ContainsKey(port.Service))
                    totalPoints -= _portPointValues[port.Service];
                else
                    totalPoints -= 10; // arbitrary midpoint value
            }
            if (totalPoints < 0) totalPoints = 0;
            report.RawPointsScored = totalPoints;
            report.MaximumPointsAvailable = 100;

            report.SetExtensionSpecificReport(nmapReport);

            return report;
        }

        private void RunSingleTarget(NativeDirectoryEvidence artifact, string targetIp)
        {
            using (NativeDirectoryContext context = artifact.GetContext())
            {
                if (DockerHostCommand.SupportsDocker())
                {
                    using var dockerCommand = new DockerHostCommand("jess/nmap:latest",
                        context,
                        "--open",
                        "-T4",
                        "-PN",
                        "-A",
                        targetIp,
                        "-n",
                        "-oX", "/synthesys/scan.xml");

                    dockerCommand.StandardOutputDataReceived += (s, taskOwner, data) => Logger.TaskLogInformation(taskOwner, data);
                    dockerCommand.StandardErrorDataReceived += (s, taskOwner, data) => Logger.TaskLogDebug(taskOwner, data);

                    dockerCommand.Start().Wait();
                }
                else
                {
                    using var hostCommand = new NativeHostCommand(
                        "nmap",
                        "--open",
                        "-T4",
                        "-PN",
                        "-A",
                        targetIp,
                        "-n",
                        "-oX", context.DirectoryWithFile("scan.xml"));

                    hostCommand.StandardOutputDataReceived += (s, taskOwner, data) => Logger.TaskLogInformation(taskOwner, data);
                    hostCommand.StandardErrorDataReceived += (s, taskOwner, data) => Logger.TaskLogDebug(taskOwner, data);

                    hostCommand.Start().Wait();
                }
            }
        }

        private XDocument GetScanXml(NativeDirectoryEvidence target)
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