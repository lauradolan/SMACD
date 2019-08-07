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
using System.Collections.Generic;
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
            try
            {
                var nativePathArtifact = this.Workspace.Artifacts.CreateOrLoadNativePath("nmap_" + Target.TargetId);
                RunScanner(nativePathArtifact);
                var jsonReport = GetJsonObject(nativePathArtifact);
                if (jsonReport == null)
                {
                    Logger.LogCritical("OWASP ZAP Scanner did not produce a report; may have been a down service!");
                    return new ZapJsonReport();
                }
                RunScorer(jsonReport);

                return jsonReport;
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Error running OWASP ZAP Scanner");
            }
            return null;
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
                            return report;
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
                    try
                    {
                        var targets = alert.Instances.Select(i => {
                            var target = new HttpTarget
                            {
                                ResourceLocatorAddress = i.Uri,
                                ResourceAccessMode = i.Method,
                                Fields = new Dictionary<string, string>()
                            };
                            var paramsSplit = i.Param.Split(',');
                            foreach (var param in paramsSplit)
                            {
                                if (param.Contains('='))
                                    target.Fields.Add(param.Split('=')[0], param.Split('=')[1]);
                                else
                                    target.Fields.Add(param, string.Empty);
                            }
                            return target;
                        }).ToList();

                        targets.ForEach(target =>
                        {
                            if (target == null)
                            {
                                Logger.LogWarning("Created Target is not valid! Skipping...");
                                return;
                            }

                            HttpTarget targetDescriptor;
                            var existingTarget = Workspace.Targets.RegisteredTargets.FirstOrDefault(r =>
                                r.Value.IsApproximateTo(ApproximationScopes.Host | ApproximationScopes.Port | ApproximationScopes.ResourceLocatorAddress, target));
                            if (existingTarget.Equals(default(KeyValuePair<string, TargetDescriptor>))) // new target
                            {
                                var newTargetName = $"{((HttpTarget)target).Method}__{((HttpTarget)target).URL}__{new Random((int)DateTime.Now.Ticks).Next(9999, Int32.MaxValue)}";
                                target.TargetId = System.Text.RegularExpressions.Regex.Replace(newTargetName, "^[a-zA-Z0-9-_]", "");
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
                    catch (Exception ex)
                    {
                        Logger.LogCritical(ex, "Error running correlations for scanner task");
                    }
                }
            // TODO: Migrate HTML report into AzDO plugin?
        }
    }
}
