using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SMACD.AppTree;
using SMACD.AppTree.Evidence;
using Synthesys.SDK;
using Synthesys.SDK.Attributes;
using Synthesys.SDK.Capabilities;
using Synthesys.SDK.Extensions;
using Synthesys.SDK.HostCommands;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Synthesys.Plugins.OwaspZap
{
    /// <summary>
    ///     The OWASP Zed Attack Proxy (ZAP) is an easy to use integrated penetration testing tool for finding vulnerabilities
    ///     in web applications. It is designed to be used by people with a wide range of security experience and as such is
    ///     ideal for developers and functional testers who are new to penetration testing as well as being a useful addition
    ///     to an experienced pen testers toolbox.
    /// </summary>
    [Extension("owaspzap",
        Name = "OWASP ZAP Scanner",
        Version = "1.0.0",
        Author = "Anthony Turner",
        Website = "https://github.com/anthturner/smacd")]
    public class OwaspZapScannerAction : ActionExtension, IOperateOnHttpService
    {
        private const string JSON_REPORT_FILE = "report.json";
        private const string HTML_REPORT_FILE = "report.html";

        /// <summary>
        ///     If <c>TRUE</c>, the "zap-full-scan" script will be used; otherwise the "zap-baseline" script is used
        /// </summary>
        [Configurable]
        public bool Aggressive { get; set; } = false;

        /// <summary>
        ///     If <c>TRUE</c>, the scan will use an AJAX-aware spider
        /// </summary>
        [Configurable]
        public bool UseAjaxSpider { get; set; } = true;

        /// <summary>
        ///     HTTP Service to scan
        /// </summary>
        public HttpServiceNode HttpService { get; set; }

        public override ExtensionReport Act()
        {
            ZapJsonReport jsonReport = null;
            try
            {
                NativeDirectoryEvidence nativePathArtifact = HttpService.Evidence.CreateOrLoadNativePath("owaspzap_" + HttpService.NiceIdentifier);
                RunScanner(nativePathArtifact);
                jsonReport = GetJsonObject(nativePathArtifact);
                if (jsonReport == null)
                {
                    Logger.LogCritical("OWASP ZAP Scanner did not produce a report; may have been a down service!");
                    return ExtensionReport.Error(new Exception("Scanner did not produce report"));
                }

                RunScorer(jsonReport);
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Error running OWASP ZAP Scanner");
            }

            ExtensionReport report = new ExtensionReport
            {
                ReportSummaryName = typeof(OwaspZapReportSummary).FullName,
                ReportViewName = typeof(OwaspZapReportView).FullName
            };
            report.SetExtensionSpecificReport(jsonReport);

            return report;
        }

        private void RunScanner(NativeDirectoryEvidence nativePathEvidence)
        {
            Logger.LogInformation("Starting OWASP ZAP Scanner against {0}", HttpService);

            string pyScript = Aggressive ? "zap-full-scan.py" : "zap-baseline.py";

            Logger.LogDebug("Using scanner script {0} (aggressive option is {1})", pyScript,
                Aggressive);

            using (NativeDirectoryContext context = nativePathEvidence.GetContext())
            {
                string schema = string.Empty;
                if (HttpService.Port == 443) // todo: this only detects ssl on standard ports, need to change this
                {
                    schema = "https";
                }
                else
                {
                    schema = "http";
                }

                if (DockerHostCommand.SupportsDocker())
                {
                    using var dockerCommand = new DockerHostCommand("owasp/zap2docker-weekly",
                        new List<string>() {
                            pyScript,
                            "-t", $"{schema}://{HttpService.Host.Hostname}:{HttpService.Port}/",
                            "-J", JSON_REPORT_FILE,
                            "-r", HTML_REPORT_FILE,
                            UseAjaxSpider ? "-j" : ""
                        },
                        new List<DockerHostMount>()
                        {
                            new DockerHostMount()
                            {
                                ContainerPath = "/zap/wrk",
                                LocalPath = context.Directory,
                                IsReadOnly = false
                            }
                        },
                        "zap");

                    dockerCommand.StandardOutputDataReceived += (s, taskOwner, data) => TranslateZapLogs(taskOwner, data);
                    dockerCommand.StandardErrorDataReceived += (s, taskOwner, data) => TranslateZapLogs(taskOwner, data);

                    dockerCommand.Start().Wait();
                }
                else
                {
                    using var hostCommand = new NativeHostCommand(
                            pyScript,
                            "-t", $"{schema}://{HttpService.Host.Hostname}:{HttpService.Port}/",
                            "-J", JSON_REPORT_FILE,
                            "-r", HTML_REPORT_FILE,
                            UseAjaxSpider ? "-j" : "");

                    hostCommand.StandardOutputDataReceived += (s, taskOwner, data) => TranslateZapLogs(taskOwner, data);
                    hostCommand.StandardErrorDataReceived += (s, taskOwner, data) => TranslateZapLogs(taskOwner, data);

                    hostCommand.Start().Wait();
                }
            }

            Logger.LogInformation("Completed OWASP ZAP scanner runtime execution");
        }

        private void TranslateZapLogs(int taskOwner, string logText)
        {
            Match match = Regex.Match(logText,
                "[0-9]+\\s+(?<src>\\w+)\\s+(?<level>\\w+)\\s+\\[(?<code>[0-9]*)\\](?<msg>.*)");
            if (match == null || !match.Success)
            {
                Logger.TaskLogDebug(taskOwner, logText);
            }
            else
            {
                string src = match.Groups["src"].Value;
                string level = match.Groups["level"].Value;
                string code = match.Groups["code"].Value;
                string msg = match.Groups["msg"].Value;

                // 0 is timestamp, 1 is source component, 2 is level, 3 is code, 4+ is text
                string logEntry = $"[{src}] {msg}";
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

        private ZapJsonReport GetJsonObject(NativeDirectoryEvidence nativePathArtifact)
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

        private void RunScorer(ZapJsonReport report)
        {
            foreach (ZapJsonSite site in report.Site)
            {
                foreach (ZapJsonAlertWithInstances alert in site.Alerts)
                {
                    try
                    {
                        System.Collections.Generic.List<UrlRequestNode> targets = alert.Instances.Select(i =>
                        {
                            UrlNode inner = UrlHelper.GeneratePathArtifacts(HttpService, i.Uri, i.Method);
                            if (inner == null)
                            {
                                Logger.LogDebug("Inner URL node not created/found, abandoning further data embedding");
                                return null;
                            }

                            UrlRequestNode artifact = new UrlRequestNode() { Parent = inner };
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

                            inner.Vulnerabilities.Add(new Vulnerability
                            {
                                Confidence = (Vulnerability.Confidences)alert.Confidence,
                                RiskLevel = (Vulnerability.RiskLevels)alert.RiskCode,
                                Description = alert.Desc,
                                Occurrences = alert.Instances.Count(),
                                Remedy = alert.Solution,
                                Title = alert.Name
                            });

                            return inner.AddRequest(i.Method, artifact.Fields, artifact.Headers);
                        }).ToList();
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