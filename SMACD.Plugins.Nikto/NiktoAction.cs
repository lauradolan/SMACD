using Microsoft.Extensions.Logging;
using SMACD.Artifacts;
using SMACD.SDK;
using SMACD.SDK.Attributes;
using SMACD.SDK.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace SMACD.Plugins.Nikto
{
    [Extension("nikto",
        Name = "Nikto Web Server Scanner",
        Version = "1.0.0",
        Author = "Anthony Turner",
        Website = "https://github.com/anthturner/smacd")]
    public class NiktoAction : ActionExtension, IOperateOnHttpService
    {
        public HttpServicePortArtifact HttpService { get; set; }

        public override ExtensionReport Act()
        {
            XDocument xml;

            using (var context = HttpService.Attachments.CreateOrLoadNativePath("nikto").GetContext())
            using (var wrapper = new ExecutionWrapper(
                "nikto " +
                "-Display 1234EP " +
               $"-host http://{HttpService.Host.Hostname}:{HttpService.Port} " +
               $"-o {context.DirectoryWithFile("scan.xml")} " +
                "-Format xml"))
            {
                wrapper.Start().Wait();

                if (!File.Exists(context.DirectoryWithFile("scan.xml")))
                {
                    Logger.LogCritical("XML report from this plugin was not found! Aborting...");
                    return null;
                }
                xml = XDocument.Load(context.DirectoryWithFile("scan.xml"));
            }

            var scanDetails = xml.Root.Descendants("scandetails");
            var report = new NiktoReport()
            {
                NiktoVersion = xml.Root.Attributes("version").First().Value,
                ScanStart = xml.Root.Attributes("scanstart").First().Value,
                ScanEnd = xml.Root.Attributes("scanend").First().Value,
                ServerBanner = scanDetails.Attributes("targetbanner").First().Value,
                SiteName = scanDetails.Attributes("sitename").First().Value
            };

            foreach (var item in scanDetails.Descendants("item"))
            {
                report.Vulnerabilities.Add(new Vulnerability()
                {
                    Title = item.Descendants("description").First().Value,
                    Occurrences = 1,
                    Confidence = Vulnerability.Confidences.Medium,
                    RiskLevel = Vulnerability.RiskLevels.Medium,
                    Description = item.Descendants("description").First().Value
                });
            }

            HttpService.Vulnerabilities.AddRange(report.Vulnerabilities);

            return report;
        }
    }

    public class NiktoReport : ExtensionReport
    {
        public string NiktoVersion { get; set; }
        public string ScanStart { get; set; }
        public string ScanEnd { get; set; }
        public string ServerBanner { get; set; }
        public string SiteName { get; set; }

        public List<Vulnerability> Vulnerabilities { get; set; } = new List<Vulnerability>();

        public override string GetReportContent()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Server Banner: {ServerBanner}");
            foreach (var vuln in Vulnerabilities)
                sb.AppendLine($"{vuln.Title} ({vuln.Occurrences} time(s)) Confidence/Risk Level: {vuln.Confidence}/{vuln.RiskLevel}");
            return sb.ToString();
        }
    }
}
