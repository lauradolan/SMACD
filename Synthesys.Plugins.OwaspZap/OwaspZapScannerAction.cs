using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SMACD.Artifacts;
using SMACD.Artifacts.Data;
using Synthesys.SDK;
using Synthesys.SDK.Attributes;
using Synthesys.SDK.Capabilities;
using Synthesys.SDK.Extensions;

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
        public HttpServicePortArtifact HttpService { get; set; }

        public override ExtensionReport Act()
        {
            try
            {
                var nativePathArtifact =
                    HttpService.Attachments.CreateOrLoadNativePath(
                        "owaspzap_" + ((HostArtifact) HttpService.Parent).IpAddress);
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
            Logger.LogInformation("Starting OWASP ZAP Scanner against {0}", HttpService);
            var wrapper = new ExecutionWrapper();

            var pyScript = Aggressive ? "zap-full-scan.py" : "zap-baseline.py";

            Logger.LogDebug("Using scanner script {0} (aggressive option is {1})", pyScript,
                Aggressive);

            var dockerCommandTemplate = "docker run " +
                                        "-v {0}:/zap/wrk:rw " +
                                        "-u zap " +
                                        "-i ictu/zap2docker-weekly " +
                                        "{1} -t {2} -J {3} -r {4} ";
            if (UseAjaxSpider) dockerCommandTemplate += "-j ";

            using (var context = nativePathArtifact.GetContext())
            {
                var schema = string.Empty;
                if (HttpService.Port == 443) // todo: this only detects ssl on standard ports, need to change this
                    schema = "https";
                else
                    schema = "http";

                Logger.LogDebug("Invoking command " + dockerCommandTemplate,
                    context.Directory, pyScript, $"{schema}://{HttpService.Host.Hostname}:{HttpService.Port}/",
                    JSON_REPORT_FILE, HTML_REPORT_FILE);
                wrapper.Command = string.Format(dockerCommandTemplate,
                    context.Directory, pyScript, $"{schema}://{HttpService.Host.Hostname}:{HttpService.Port}/",
                    JSON_REPORT_FILE, HTML_REPORT_FILE);

                wrapper.StandardOutputDataReceived += (s, taskOwner, data) => TranslateZapLogs(taskOwner, data);
                wrapper.StandardErrorDataReceived += (s, taskOwner, data) => TranslateZapLogs(taskOwner, data);

                wrapper.Start().Wait();
            }

            Logger.LogInformation("Completed OWASP ZAP scanner runtime execution");
        }

        private void TranslateZapLogs(int taskOwner, string logText)
        {
            var match = Regex.Match(logText,
                "[0-9]+\\s+(?<src>\\w+)\\s+(?<level>\\w+)\\s+\\[(?<code>[0-9]*)\\](?<msg>.*)");
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
            using (var context = nativePathArtifact.GetContext())
            {
                var jsonReportPath = context.DirectoryWithFile(JSON_REPORT_FILE);
                if (File.Exists(jsonReportPath))
                    try
                    {
                        using (var sr = new StreamReader(jsonReportPath))
                        {
                            report = JsonConvert.DeserializeObject<ZapJsonReport>(sr.ReadToEnd());
                            if (report == null)
                                Logger.LogCritical("JSON report from this plugin was not valid! Aborting...");
                            return report;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogCritical(ex, "Error deserializing OWASP ZAP JSON output report!");
                    }
                else
                    Logger.LogCritical("JSON report from this plugin was not found! Aborting...");
            }

            return null;
        }

        private void RunScorer(ZapJsonReport report)
        {
            foreach (var site in report.Site)
            foreach (var alert in site.Alerts)
                try
                {
                    var targets = alert.Instances.Select(i =>
                    {
                        var inner = UrlHelper.GeneratePathArtifacts(HttpService, i.Uri, i.Method);

                        var artifact = new UrlRequestArtifact();
                        if (i.Param != null)
                        {
                            var paramsSplit = i.Param.Split(',');
                            foreach (var param in paramsSplit)
                                if (param.Contains('='))
                                    artifact.Fields.Add(param.Split('=')[0], param.Split('=')[1]);
                                else
                                    artifact.Fields.Add(param, string.Empty);
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
                            Confidence = (Vulnerability.Confidences) alert.Confidence,
                            RiskLevel = (Vulnerability.RiskLevels) alert.RiskCode,
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

            // TODO: Migrate HTML report into AzDO plugin?
        }
    }
}