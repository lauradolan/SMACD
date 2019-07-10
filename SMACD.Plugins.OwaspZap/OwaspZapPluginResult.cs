using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SMACD.Shared;
using SMACD.Shared.Data;
using SMACD.Shared.Plugins;
using SMACD.Shared.Resources;
using SMACD.Shared.WorkspaceManagers;

namespace SMACD.Plugins.OwaspZap
{
    public class OwaspZapPluginResult : PluginResult
    {
        public OwaspZapPluginResult()
        {
        }

        public OwaspZapPluginResult(string workingDirectory) : base(workingDirectory)
        {
        }

        public OwaspZapPluginResult(PluginPointerModel pluginPointer, string workingDirectory) : base(pluginPointer,
            workingDirectory)
        {
            SaveResultArtifact(Path.Combine(workingDirectory, ".ptr"), pluginPointer);
        }

        private ILogger Logger { get; } = Extensions.LogFactory.CreateLogger("OwaspZapPluginResult");

        public override async Task SummaryRunOnce(VulnerabilitySummary summary)
        {
            ZapJsonReport report = null;
            var jsonReportPath = Path.Combine(WorkingDirectory, OwaspZapPlugin.JSON_REPORT_FILE);
            if (File.Exists(jsonReportPath))
            {
                using (var sr = new StreamReader(jsonReportPath))
                {
                    report = JsonConvert.DeserializeObject<ZapJsonReport>(await sr.ReadToEndAsync());
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

            foreach (var alert in report.Site.First().Alerts)
            {
                var item = new VulnerabilityItem
                {
                    PluginResults = new List<PluginResult> {this},
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
                    summary.VulnerabilityItems.Add(item);
                else // correlation -- multiple plugins see this
                {
                    var correlatedItem = summary.VulnerabilityItems.FirstOrDefault(v =>
                        v.Fingerprint(skippedFields: skipFields) == fingerprint);
                    if (correlatedItem != null && !correlatedItem.PluginResults.Contains(this))
                    {
                        correlatedItem.PluginResults.Add(this);
                        correlatedItem.Extras["OWASPZAP"] = item.Extras["OWASPZAP"];
                    }
                }
            }

            // TODO: Migrate HTML report into AzDO plugin?
        }

        public override async Task<bool> SummaryRunGenerationally(VulnerabilitySummary summary)
        {
            // Nothing to converge!
            return await Task.FromResult(false);
        }
    }
}