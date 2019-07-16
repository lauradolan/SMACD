using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SMACD.ScannerEngine;
using SMACD.ScannerEngine.Attributes;
using SMACD.ScannerEngine.Extensions;
using SMACD.ScannerEngine.Plugins;
using SMACD.ScannerEngine.Resources;

namespace SMACD.Plugins.OwaspZap
{
    [AllowResourceType(typeof(HttpResource))]
    [AllowResourceType(typeof(HttpsResource))]
    [PluginMetadata("owaspzap", Name = "OWASP ZAP Scanner", DefaultScorer = "owaspzap")]
    public class OwaspZapAttackTool : AttackToolPlugin
    {
        internal const string JSON_REPORT_FILE = "report.json";
        internal const string HTML_REPORT_FILE = "report.html";

        [Configurable] public bool Aggressive { get; set; } = false;

        [Configurable] public bool UseAjaxSpider { get; set; } = true;

        public override async Task Execute()
        {
            Logger = Global.LogFactory.CreateLogger("OwaspZap@" + Pointer.Resource.ResourceId);
            Logger.LogInformation("Starting OWASP ZAP plugin against Resource {0} with working directory {1}",
                Pointer.Resource?.ResourceId, WorkingDirectory);
            var httpTarget = ResourceManager.Instance.GetByPointer(Pointer.Resource) as HttpResource;
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
                WorkingDirectory, pyScript, httpTarget.Url, JSON_REPORT_FILE, HTML_REPORT_FILE);
            wrapper.Command = string.Format(dockerCommandTemplate,
                WorkingDirectory, pyScript, httpTarget.Url, JSON_REPORT_FILE, HTML_REPORT_FILE);

            wrapper.StandardOutputDataReceived += (s, taskOwner, data) => Logger.TaskLogInformation(taskOwner, data);
            wrapper.StandardErrorDataReceived += (s, taskOwner, data) => Logger.TaskLogDebug(taskOwner, data);
            wrapper.Start(Pointer).Wait();

            Logger.LogInformation("Completed OWASP ZAP scanner runtime execution in {0}", wrapper.ExecutionTime);
        }
    }
}