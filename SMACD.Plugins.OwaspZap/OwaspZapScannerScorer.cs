using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SMACD.Data;
using SMACD.ScannerEngine;
using SMACD.ScannerEngine.Attributes;
using SMACD.ScannerEngine.Extensions;
using SMACD.ScannerEngine.Plugins;
using SMACD.ScannerEngine.Resources;

namespace SMACD.Plugins.OwaspZap
{
    [PluginMetadata("owaspzap", Name = "OWASP ZAP Scanner Default Scorer")]
    public class OwaspZapScannerScorer : ScorerPlugin
    {
        public OwaspZapScannerScorer()
        {
            Logger = Global.LogFactory.CreateLogger("OwaspZapScannerScorer");
        }

        public override async Task Score(VulnerabilitySummary summary)
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
                    PluginPointer = Pointer,
                    PluginRawScore = alert.RiskCode * alert.Confidence,
                    PluginAdjustedScore = alert.RiskCode * alert.Confidence / (3.0 * 5.0) * 100,
                    Description = alert.Desc,
                    Resources = alert.Instances.Select(instance =>
                    {
                        var newResource = new HttpResource {Method = instance.Method, Url = instance.Uri};
                        var newFingerprint =
                            newResource.Fingerprint(skippedFields: new[] {"resourceId", "fields", "headers"});
                        if (ResourceManager.Instance.ContainsFingerprint(newFingerprint))
                            return (HttpResource) ResourceManager.Instance.GetByFingerprint(newFingerprint);

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

        public override async Task<bool> Converge(VulnerabilitySummary summary)
        {
            // Nothing to converge!
            return false;
        }
    }
}