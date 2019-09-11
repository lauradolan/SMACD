using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using SMACD.Artifacts;
using Synthesys.SDK;
using Synthesys.SDK.Attributes;
using Synthesys.SDK.Capabilities;
using Synthesys.SDK.Extensions;

namespace Synthesys.Plugins.Nikto
{
    /// <summary>
    /// Nikto is an Open Source (GPL) web server scanner which performs comprehensive tests against web servers for multiple items, including over 6700 potentially dangerous files/programs, checks for outdated versions of over 1250 servers, and version specific problems on over 270 servers.
    /// </summary>
    /// <remarks>Description from tools.kali.org</remarks>
    [Extension("nikto",
        Name = "Nikto Web Server Scanner",
        Version = "1.0.0",
        Author = "Anthony Turner",
        Website = "https://github.com/anthturner/smacd")]
    public class NiktoAction : ActionExtension, IOperateOnHttpService
    {
        private const int BASE_ITEM_WEIGHT = 2;
        private const int OSVDB_BOUND_ITEM_WEIGHT = 5;
        private const int NO_BOUND_ITEM_WEIGHT = 5;

        /// <summary>
        /// HTTP Service being scanned
        /// </summary>
        public HttpServicePortArtifact HttpService { get; set; }

        public override ExtensionReport Act()
        {
            var xml = ExecuteScanner();
            if (xml == null) return ExtensionReport.Error(new Exception("Failed to generate XML"));

            var scanDetails = xml.Root.Descendants("scandetails");
            var report = new NiktoReport()
            {
                NiktoVersion = xml.Root.Attributes("version").First().Value,
                ScanStart = xml.Root.Attributes("scanstart").First().Value,
                ScanEnd = xml.Root.Attributes("scanend").First().Value,
                ServerBanner = scanDetails.Attributes("targetbanner").First().Value,
                SiteName = scanDetails.Attributes("sitename").First().Value
            };

            var itemsTested = scanDetails.Descendants("statistics").Attributes("itemstested").First().Value;
            report.MaximumPointsAvailable = int.Parse(itemsTested) * BASE_ITEM_WEIGHT;

            foreach (var item in scanDetails.Descendants("item"))
            {
                var osvdbid = int.Parse(item.Attributes("osvdbid").First().Value);
                var link = item.Descendants("namelink").First().Value;
                var method = item.Attributes("method").First().Value;
                var urlLeaf = UrlHelper.GeneratePathArtifacts(HttpService, link, method);

                if (osvdbid > 0) report.RawPointsScored += OSVDB_BOUND_ITEM_WEIGHT;
                else report.RawPointsScored += NO_BOUND_ITEM_WEIGHT;

                var vulnerability = new Vulnerability()
                {
                    Title = item.Descendants("description").First().Value,
                    Occurrences = 1,
                    Confidence = Vulnerability.Confidences.Medium,
                    RiskLevel = osvdbid > 0 ? Vulnerability.RiskLevels.Medium : Vulnerability.RiskLevels.Low,
                    Description = item.Descendants("description").First().Value
                };
                report.Vulnerabilities.Add(vulnerability);
                urlLeaf.Vulnerabilities.Add(vulnerability);
            }

            HttpService.Vulnerabilities.AddRange(report.Vulnerabilities);

            return report;
        }

        private XDocument ExecuteScanner()
        {
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
                return XDocument.Load(context.DirectoryWithFile("scan.xml"));
            }
        }
    }
}
