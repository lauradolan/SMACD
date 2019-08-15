using Microsoft.Extensions.Logging;
using SMACD.Artifacts;
using SMACD.Artifacts.Data;
using SMACD.SDK;
using SMACD.SDK.Attributes;
using SMACD.SDK.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Xml.Linq;

namespace SMACD.Plugins.Nmap
{
    [Extension("nmap",
        Name = "NMap Port Scanner",
        Version = "1.0.0",
        Author = "Anthony Turner",
        Website = "https://github.com/anthturner/smacd")]
    public class NmapScannerAction : ActionExtension, IOperateOnHost
    {
        public HostArtifact Host { get; set; }

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
            }
            foreach (Vulnerability vuln in report.Vulnerabilities)
            {
                Host.Vulnerabilities.Add(vuln);
            }

            return report;
        }

        private void RunSingleTarget(NativeDirectoryArtifact artifact, string targetIp)
        {

            using (NativeDirectoryContext context = artifact.GetContext())
            {
//                string cmd = $"nmap --open -T4 -PN {targetIp} -n -oX {context.DirectoryWithFile("scan.xml")}";
                string cmd = $"C:\\Progra~2\\Nmap\\nmap.exe --open -6 -T4 -PN {targetIp} -n -oX {context.DirectoryWithFile("scan.xml")}";

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
                string start = xml.Root.Attributes("start").First().Value;
                result.TimeOfExecution = DateTime.FromFileTime(long.Parse(start));

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

                        Vulnerability.Confidences confidenceEnum = (Vulnerability.Confidences)int.Parse(conf);
                        result.Vulnerabilities.Add(new Vulnerability
                        {
                            Confidence = confidenceEnum,
                            RiskLevel = Vulnerability.RiskLevels.Informational,
                            Description =
                                $"NMap found an open port {protocol} {port} on {addr}. NMap's guess for this service is {service} (confidence: {conf} - {confidenceEnum})",
                            Occurrences = 1,
                            Remedy =
                                "If this port should be open to provide a service, there is no need for a change. Otherwise, find out if this port needs to be opened, and if not, " +
                                "terminate the service using it, or apply firewall rules to prevent its access from the open Internet.",
                            Title = $"{protocol} {port} open" + (service == null ? "" : $" ({service})"),
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