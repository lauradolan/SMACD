using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SMACD.Shared;
using SMACD.Shared.Attributes;
using SMACD.Shared.Extensions;
using SMACD.Shared.Plugins.Scorers;
using SMACD.Shared.Resources;

namespace SMACD.Plugins.OwaspZap
{
    [ScorerMetadata("owaspzap", Name = "OWASP ZAP Scanner Default Scorer")]
    public class OwaspZapScannerScorer : Scorer
    {
        public OwaspZapScannerScorer(string workingDirectory) : base(workingDirectory)
        {
            Logger = Workspace.LogFactory.CreateLogger("OwaspZapScannerScorer");
        }
 
        public override async Task GenerateScore(VulnerabilitySummary summary)
        {
            ZapJsonReport report = null;
            var jsonReportPath = Path.Combine(WorkingDirectory, OwaspZapAttackTool.JSON_REPORT_FILE);
            if (File.Exists(jsonReportPath))
            {
                try
                {
                    using (var sr = new StreamReader(jsonReportPath))
                    {
                        report = JsonConvert.DeserializeObject<ZapJsonReport>(await sr.ReadToEndAsync());
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
                return;
            }

            if (report == null)
            {
                Logger.LogCritical("JSON report from this plugin was not valid! Aborting...");
                return;
            }

            foreach (var site in report.Site)
            foreach (var alert in site.Alerts)
            {
                var item = new VulnerabilityItem
                {
                    //PluginResults = new List<ScannerReportAggregator> {this},
                    PluginPointer = PluginPointer,
                    PluginRawScore = alert.RiskCode * alert.Confidence,
                    PluginAdjustedScore = alert.RiskCode * alert.Confidence / (3.0 * 5.0) * 100,
                    Description = alert.Desc,
                    Resources = alert.Instances.Select(instance =>
                    {
                        var newResource = new HttpResource {Method = instance.Method, Url = instance.Uri};
                        var newFingerprint =
                            newResource.Fingerprint(skippedFields: new[] {"resourceId", "fields", "headers"});
                        if (ResourceManager.Instance.ContainsFingerprint(newFingerprint))
                            return ResourceManager.Instance.GetByFingerprint<HttpResource>(newFingerprint);

                        ResourceManager.Instance.Register(newResource);
                        return newResource;
                    }).Cast<Resource>().ToList()
                };

                item.Extras["OWASPZAP"] = new
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

                string[] skipFields = {"pluginResults", "extras"};
                var fingerprint = item.Fingerprint(skippedFields: skipFields);
                if (summary.VulnerabilityItems.All(i => i.Fingerprint(skippedFields: skipFields) != fingerprint))
                {
                    summary.VulnerabilityItems.Add(item);
                }
                else // correlation -- multiple plugins see this
                {
                    var correlatedItem = summary.VulnerabilityItems.FirstOrDefault(v =>
                        v.Fingerprint(skippedFields: skipFields) == fingerprint);
                    //if (correlatedItem != null && !correlatedItem.PluginResults.Contains(this))
                    //{
                    //    correlatedItem.PluginResults.Add(this);
                    //    correlatedItem.Extras["OWASPZAP"] = item.Extras["OWASPZAP"];
                    //}
                }
            }

            // TODO: Migrate HTML report into AzDO plugin?
        }

        public override async Task<bool> ConvergeSummary(VulnerabilitySummary summary)
        {
            // Nothing to converge!
            return await Task.FromResult(false);
        }
    }
}