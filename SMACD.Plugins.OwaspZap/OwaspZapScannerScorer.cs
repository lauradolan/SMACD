using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SMACD.PluginHost.Attributes;
using SMACD.PluginHost.Extensions;
using SMACD.PluginHost.Plugins;
using SMACD.PluginHost.Reports;
using SMACD.PluginHost.Resources;
using System;
using System.Collections;
using System.IO;
using System.Linq;

namespace SMACD.Plugins.OwaspZap
{
    [PluginImplementation(PluginTypes.Scorer, "owaspzap")]
    public class OwaspZapScannerScorer : Plugin
    {
        public OwaspZapScannerScorer(string workingDirectory) : base(workingDirectory)
        {
        }

        public override ScoredResult Execute()
        {
            var result = CreateBlankScoredResult();
            ZapJsonReport report = null;
            var jsonReportPath = WorkingDirectory.ParentResource.GetMostRecent(PluginTypes.AttackTool).WithFile(OwaspZapAttackTool.JSON_REPORT_FILE);
            if (File.Exists(jsonReportPath))
            {
                try
                {
                    using (var sr = new StreamReader(jsonReportPath))
                    {
                        report = JsonConvert.DeserializeObject<ZapJsonReport>(sr.ReadToEnd());
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogCritical(ex, "Error deserializing OWASP ZAP JSON output report!");
                }
            }
            else
            {
                Logger.LogCritical("JSON report from this plugin was not found! Aborting...");
                return CreateBlankScoredResult();
            }

            if (report == null)
            {
                Logger.LogCritical("JSON report from this plugin was not valid! Aborting...");
                return CreateBlankScoredResult();
            }

            foreach (var site in report.Site)
                foreach (var alert in site.Alerts)
                {
                    var resources = alert.Instances.Select(i => new HttpResource
                    {
                        Url = i.Uri,
                        Method = i.Method,
                        Fields = i.Param.Split(',').Select(set => Tuple.Create(set.Split('=')[0], set.Split('=')[1]))
                            .ToDictionary(k => k.Item1, v => v.Item2)
                    }).ToList();

                    resources.ForEach(resource =>
                    {
                        var knownResource = ResourceManager.Instance.Search(r => r.IsApproximateTo(resource));
                        if (knownResource == null) // new resource
                        ResourceManager.Instance.Register(resource);

                        ((IList)result.Vulnerabilities).Add(new Vulnerability
                        {
                            Target = resource,
                            Confidence = (Vulnerability.Confidences)alert.Confidence,
                            RiskLevel = (Vulnerability.RiskLevels)alert.RiskCode,
                            Description = alert.Desc,
                            Occurrences = alert.Instances.Count(),
                            Remedy = alert.Solution,
                            ShortName = alert.Name
                        });
                    });

                    // Will be cleaned up for duplicates later
                    result.Plugin.ResourceIds.AddRange(resources.Select(r => r.ResourceId));

                    result.Extra["OWASPZAP"] = new
                    {
                        alert.Alert,
                        alert.Confidence,
                        alert.Count,
                        alert.CWEId,
                        alert.Desc,
                        alert.Name,
                        alert.OtherInfo,
                        alert.PluginId,
                        alert.Reference,
                        alert.RiskCode,
                        alert.RiskDesc,
                        alert.Solution,
                        alert.SourceId,
                        alert.WASCId
                    };
                }

            return result;
            // TODO: Migrate HTML report into AzDO plugin?
        }
    }
}