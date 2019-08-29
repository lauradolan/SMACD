using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SMACD.Artifacts;
using SMACD.Artifacts.Data;
using SMACD.SDK;
using SMACD.SDK.Attributes;
using SMACD.SDK.Capabilities;
using SMACD.SDK.Extensions;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace SMACD.Plugins.OwaspZap
{
    [Extension("owaspzap",
        Name = "OWASP ZAP Scanner",
        Version = "1.0.0",
        Author = "Anthony Turner",
        Website = "https://github.com/anthturner/smacd")]
    public class OwaspZapScannerAction : ActionExtension, IOperateOnHttpService
    {
        private const string JSON_REPORT_FILE = "report.json";
        private const string HTML_REPORT_FILE = "report.html";

        [Configurable] public bool Aggressive { get; set; } = false;

        [Configurable] public bool UseAjaxSpider { get; set; } = true;

        public HttpServicePortArtifact HttpService { get; set; }

        public override ExtensionReport Act()
        {
            try
            {
                NativeDirectoryArtifact nativePathArtifact = HttpService.Attachments.CreateOrLoadNativePath("owaspzap_" + ((HostArtifact)HttpService.Parent).IpAddress);
                RunScanner(nativePathArtifact);
                ZapJsonReport jsonReport = GetJsonObject(nativePathArtifact);
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
            Logger.LogInformation("Starting OWASP ZAP Scanner against {0}", HttpService);
            ExecutionWrapper wrapper = new ExecutionWrapper();

            string pyScript = Aggressive ? "zap-full-scan.py" : "zap-baseline.py";

            Logger.LogDebug("Using scanner script {0} (aggressive option is {1})", pyScript,
                Aggressive);

            string dockerCommandTemplate = "docker run " +
                                        "-v {0}:/zap/wrk:rw " +
                                        "-u zap " +
                                        "-i ictu/zap2docker-weekly " +
                                        "{1} -t {2} -J {3} -r {4} ";
            if (UseAjaxSpider)
            {
                dockerCommandTemplate += "-j ";
            }

            using (NativeDirectoryContext context = nativePathArtifact.GetContext())
            {
                string schema = string.Empty;
                if (HttpService.Port == 443) // todo: this only detects ssl on standard ports, need to change this
                    schema = "https";
                else
                    schema = "http";

                Logger.LogDebug("Invoking command " + dockerCommandTemplate,
                    context.Directory, pyScript, $"{schema}://{HttpService.Host.Hostname}:{HttpService.Port}/", JSON_REPORT_FILE, HTML_REPORT_FILE);
                wrapper.Command = string.Format(dockerCommandTemplate,
                    context.Directory, pyScript, $"{schema}://{HttpService.Host.Hostname}:{HttpService.Port}/", JSON_REPORT_FILE, HTML_REPORT_FILE);

                wrapper.StandardOutputDataReceived += (s, taskOwner, data) => TranslateZapLogs(taskOwner, data);
                wrapper.StandardErrorDataReceived += (s, taskOwner, data) => TranslateZapLogs(taskOwner, data);

                wrapper.Start().Wait();
            }

            Logger.LogInformation("Completed OWASP ZAP scanner runtime execution");
        }

        private void TranslateZapLogs(int taskOwner, string logText)
        {
            var match = Regex.Match(logText, "[0-9]+\\s+(?<src>\\w+)\\s+(?<level>\\w+)\\s+\\[(?<code>[0-9]*)\\](?<msg>.*)");
            if (match == null || !match.Success)
            {
                Logger.TaskLogDebug(taskOwner, logText);
            }
            else
            {
                var src = match.Groups["src"].Value;
                var level = match.Groups["level"].Value;
                var code = match.Groups["code"].Value;
                var msg = match.Groups["msg"].Value;

                // 0 is timestamp, 1 is source component, 2 is level, 3 is code, 4+ is text
                var logEntry = $"[{src}] {msg}";
                switch (level.Trim())
                {
                    case "TRACE":
                        Logger.TaskLogTrace(taskOwner, logEntry);
                        break;
                    default:
                    case "DEBUG":
                        Logger.TaskLogDebug(taskOwner, logEntry);
                        break;
                    case "INFO":
                        Logger.TaskLogInformation(taskOwner, logEntry);
                        break;
                    case "WARN":
                        Logger.TaskLogWarning(taskOwner, logEntry);
                        break;
                    case "ERROR":
                        Logger.TaskLogError(taskOwner, logEntry);
                        break;
                    case "FATAL":
                        Logger.TaskLogCritical(taskOwner, logEntry);
                        break;
                }
            }
        }

        private ZapJsonReport GetJsonObject(NativeDirectoryArtifact nativePathArtifact)
        {
            ZapJsonReport report;
            using (NativeDirectoryContext context = nativePathArtifact.GetContext())
            {
                string jsonReportPath = context.DirectoryWithFile(JSON_REPORT_FILE);
                if (File.Exists(jsonReportPath))
                {
                    try
                    {
                        using (StreamReader sr = new StreamReader(jsonReportPath))
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

        private UrlArtifact GeneratePathArtifacts(ZapJsonAlertInstance instance)
        {
            string[] pieces = instance.Uri.Split('/');
            UrlArtifact artifact = HttpService["/"];
            foreach (string piece in pieces)
            {
                if (pieces.Last() == piece)
                {
                    if (instance.Method.ToUpper() == "GET")
                    {
                        artifact[piece].Method = HttpMethod.Get;
                    }
                    else if (instance.Method.ToUpper() == "POST")
                    {
                        artifact[piece].Method = HttpMethod.Post;
                    }
                    else if (instance.Method.ToUpper() == "PUT")
                    {
                        artifact[piece].Method = HttpMethod.Put;
                    }
                    else if (instance.Method.ToUpper() == "DELETE")
                    {
                        artifact[piece].Method = HttpMethod.Delete;
                    }
                    else if (instance.Method.ToUpper() == "HEAD")
                    {
                        artifact[piece].Method = HttpMethod.Head;
                    }
                    else if (instance.Method.ToUpper() == "TRACE")
                    {
                        artifact[piece].Method = HttpMethod.Trace;
                    }
                }
                artifact = artifact[piece];
            }
            return artifact;
        }

        private void RunScorer(ZapJsonReport report)
        {
            foreach (ZapJsonSite site in report.Site)
            {
                foreach (ZapJsonAlertWithInstances alert in site.Alerts)
                {
                    try
                    {
                        System.Collections.Generic.List<UrlRequestArtifact> targets = alert.Instances.Select(i =>
                        {
                            UrlArtifact inner = GeneratePathArtifacts(i);

                            UrlRequestArtifact artifact = new UrlRequestArtifact();
                            if (i.Param != null)
                            {
                                string[] paramsSplit = i.Param.Split(',');
                                foreach (string param in paramsSplit)
                                {
                                    if (param.Contains('='))
                                    {
                                        artifact.Fields.Add(param.Split('=')[0], param.Split('=')[1]);
                                    }
                                    else
                                    {
                                        artifact.Fields.Add(param, string.Empty);
                                    }
                                }
                            }

                            inner.Requests.Add(artifact);

                            return artifact;
                        }).ToList();

                        targets.ForEach(target =>
                        {
                            if (target == null)
                            {
                                Logger.LogWarning("Created Target is not valid! Skipping...");
                                return;
                            }

                            HttpService.Vulnerabilities.Add(new Vulnerability
                            {
                                Confidence = (Vulnerability.Confidences)alert.Confidence,
                                RiskLevel = (Vulnerability.RiskLevels)alert.RiskCode,
                                Description = alert.Desc,
                                Occurrences = alert.Instances.Count(),
                                Remedy = alert.Solution,
                                Title = alert.Name
                            });
                        });
                    }
                    catch (Exception ex)
                    {
                        Logger.LogCritical(ex, "Error running correlations for scanner task");
                    }
                }
            }
            // TODO: Migrate HTML report into AzDO plugin?
        }
    }
}
