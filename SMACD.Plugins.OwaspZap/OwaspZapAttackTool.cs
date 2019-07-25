using Microsoft.Extensions.Logging;
using SMACD.PluginHost.Attributes;
using SMACD.PluginHost.Extensions;
using SMACD.PluginHost.Plugins;
using SMACD.PluginHost.Reports;
using SMACD.PluginHost.Resources;

namespace SMACD.Plugins.OwaspZap
{
    [PluginImplementation(PluginTypes.AttackTool, "owaspzap")]
    public class OwaspZapAttackTool : Plugin
    {
        internal const string JSON_REPORT_FILE = "report.json";
        internal const string HTML_REPORT_FILE = "report.html";

        [Configurable] public bool Aggressive { get; set; } = false;

        [Configurable] public bool UseAjaxSpider { get; set; } = true;

        public HttpResource Resource { get; set; }

        public OwaspZapAttackTool(string workingDirectory) : base(workingDirectory)
        {

        }

        public override ScoredResult Execute()
        {
            Logger.LogInformation("Starting OWASP ZAP plugin against {0} with working directory {1}",
                Resource.ResourceId, WorkingDirectory);
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

            Logger.LogDebug("Invoking command " + dockerCommandTemplate,
                WorkingDirectory, pyScript, Resource.Url, JSON_REPORT_FILE, HTML_REPORT_FILE);
            wrapper.Command = string.Format(dockerCommandTemplate,
                WorkingDirectory, pyScript, Resource.Url, JSON_REPORT_FILE, HTML_REPORT_FILE);

            wrapper.StandardOutputDataReceived += (s, taskOwner, data) => Logger.TaskLogInformation(taskOwner, data);
            wrapper.StandardErrorDataReceived += (s, taskOwner, data) => Logger.TaskLogDebug(taskOwner, data);
            wrapper.Start().Wait();

            Logger.LogInformation("Completed OWASP ZAP scanner runtime execution in {0}", wrapper.ExecutionTime);

            return this.GetScoredResult();
        }
    }
}