using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SMACD.Workspace;
using SMACD.Workspace.Actions;
using SMACD.Workspace.Artifacts;
using SMACD.Workspace.Customizations;
using SMACD.Workspace.Customizations.Correlations;
using SMACD.Workspace.Libraries.Attributes;
using SMACD.Workspace.Targets;
using System;
using System.IO;
using System.Linq;

namespace SMACD.Plugins.OwaspZap
{
    [Implementation(SMACD.Workspace.Libraries.ExtensionRoles.Producer, "owaspzap")]
    public class OwaspZapScannerAction : ActionInstance
    {
        private const string JSON_REPORT_FILE = "report.json";
        private const string HTML_REPORT_FILE = "report.html";

        [Configurable] public bool Aggressive { get; set; } = false;

        [Configurable] public bool UseAjaxSpider { get; set; } = true;

        public HttpTarget Target { get; set; }

        public override ActionSpecificReport Execute()
        {
            var nativePathArtifact = this.Workspace.Artifacts.CreateOrLoadNativePath("nmap_" + Target.TargetId);
            RunScanner(nativePathArtifact);
            var jsonReport = GetJsonObject(nativePathArtifact);
            RunScorer(jsonReport);

            return jsonReport;
        }

        private void RunScanner(NativeDirectoryArtifact nativePathArtifact)
        { 
            Logger.LogInformation("Starting OWASP ZAP Scanner against {0}", Target.TargetId);
            var wrapper = new ExecutionWrapper();

            var pyScript = Aggressive ? "zap-full-scan.py" : "zap-baseline.py";

            Logger.LogDebug("Using scanner script {0} (aggressive option is {1})", pyScript,
                Aggressive);

            var dockerCommandTemplate = "docker run " +
                                        "-v {0}:/zap/wrk:rw " +
                                        "-u zap " +
                                        "-i ictu/zap2docker-weekly " +
                                        "{1} -t {2} -J {3} -r {4} ";
            if (UseAjaxSpider)
                dockerCommandTemplate += "-j ";
            
            
            using (var context = nativePathArtifact.GetContext())
            {
                Logger.LogDebug("Invoking command " + dockerCommandTemplate,
                    context.Directory, pyScript, Target.ResourceLocatorAddress, JSON_REPORT_FILE, HTML_REPORT_FILE);
                wrapper.Command = string.Format(dockerCommandTemplate,
                    context.Directory, pyScript, Target.ResourceLocatorAddress, JSON_REPORT_FILE, HTML_REPORT_FILE);

                wrapper.StandardOutputDataReceived += (s, taskOwner, data) => Logger.TaskLogInformation(taskOwner, data);
                wrapper.StandardErrorDataReceived += (s, taskOwner, data) => Logger.TaskLogDebug(taskOwner, data);

                wrapper.Start().Wait();
            }

            Logger.LogInformation("Completed OWASP ZAP scanner runtime execution");
        }

        private ZapJsonReport GetJsonObject(NativeDirectoryArtifact nativePathArtifact)
        {
            ZapJsonReport report;
            using (var context = nativePathArtifact.GetContext())
            {
                var jsonReportPath = context.DirectoryWithFile(JSON_REPORT_FILE);
                if (File.Exists(jsonReportPath))
                {
                    try
                    {
                        using (var sr = new StreamReader(jsonReportPath))
                        {
                            report = JsonConvert.DeserializeObject<ZapJsonReport>(sr.ReadToEnd());
                            if (report == null)
                            {
                                Logger.LogCritical("JSON report from this plugin was not valid! Aborting...");
                            }
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
                }
            }
            return null;
        }

        private void RunScorer(ZapJsonReport report)
        {
            foreach (var site in report.Site)
                foreach (var alert in site.Alerts)
                {
                    var targets = alert.Instances.Select(i => new HttpTarget
                    {
                        ResourceLocatorAddress = i.Uri,
                        ResourceAccessMode = i.Method,
                        Fields = i.Param.Split(',').Select(set => Tuple.Create(set.Split('=')[0], set.Split('=')[1]))
                            .ToDictionary(k => k.Item1, v => v.Item2)
                    }).ToList();

                    targets.ForEach(target =>
                    {
                        HttpTarget targetDescriptor;
                        var existingTarget = Workspace.Targets.RegisteredTargets.FirstOrDefault(r =>
                            r.Value.IsApproximateTo(ApproximationScopes.Host | ApproximationScopes.Port | ApproximationScopes.ResourceLocatorAddress, target));
                        if (existingTarget.Key == null) // new target
                        {
                            Workspace.Targets.RegisterTarget(target);
                            targetDescriptor = target;
                        }
                        else
                            targetDescriptor = existingTarget.Value as HttpTarget;

                        Workspace.Correlations()
                            .WithHost(targetDescriptor.RemoteHost)
                            .Vulnerabilities
                            .AddVulnerability(new Vulnerability
                            {
                                Target = target,
                                Confidence = (Vulnerability.Confidences)alert.Confidence,
                                RiskLevel = (Vulnerability.RiskLevels)alert.RiskCode,
                                Description = alert.Desc,
                                Occurrences = alert.Instances.Count(),
                                Remedy = alert.Solution,
                                ShortName = alert.Name
                            });
                    });
                }
            // TODO: Migrate HTML report into AzDO plugin?
        }
    }
}
