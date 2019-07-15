using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SMACD.Shared;
using SMACD.Shared.Attributes;
using SMACD.Shared.Data;
using SMACD.Shared.Extensions;
using SMACD.Shared.Plugins;
using SMACD.Shared.Plugins.AttackTools;
using SMACD.Shared.Resources;

namespace SMACD.Plugins.OwaspZap
{
    [ValidResources(typeof(HttpResource), typeof(HttpsResource))]
    [ConfigurableOption(OptionName = "aggressive", Default = "false")]
    [ConfigurableOption(OptionName = "useajaxspider", Default = "true")]
    [AttackToolMetadata("owaspzap", Name = "OWASP ZAP Scanner", DefaultScorer = "owaspzap")]
    public class OwaspZapAttackTool : AttackTool
    {
        internal const string JSON_REPORT_FILE = "report.json";
        internal const string HTML_REPORT_FILE = "report.html";

        public override async Task Execute(PluginPointerModel pointer, string workingDirectory)
        {
            Logger = Workspace.LogFactory.CreateLogger("OwaspZap@" + pointer.Resource.ResourceId);
            Logger.LogInformation("Starting OWASP ZAP plugin against Resource {0} with working directory {1}",
                pointer.Resource?.ResourceId, workingDirectory);
            var httpTarget = ResourceManager.Instance.GetByPointer(pointer.Resource) as HttpResource;
            var wrapper = new ExecutionWrapper();

            string pyScript;
            if (GetOption(pointer.PluginParameters, "aggressive") == "false")
                pyScript = "zap-baseline.py";
            else
                pyScript = "zap-full-scan.py";
            Logger.LogDebug("Using scanner script {0} (aggressive option is {1})", pyScript,
                GetOption(pointer.PluginParameters, "aggressive"));

            var dockerCommandTemplate = "docker run " +
                                        "-v {0}:/zap/wrk:rw " +
                                        "-u zap " +
                                        "-i ictu/zap2docker-weekly " +
                                        "{1} -t {2} -J {3} -r {4} ";
            if (GetOption(pointer.PluginParameters, "useajaxspider") == "true")
                dockerCommandTemplate += "-j ";

            Logger.LogDebug("Invoking command " + dockerCommandTemplate,
                workingDirectory, pyScript, httpTarget.Url, JSON_REPORT_FILE, HTML_REPORT_FILE);
            wrapper.Command = string.Format(dockerCommandTemplate,
                workingDirectory, pyScript, httpTarget.Url, JSON_REPORT_FILE, HTML_REPORT_FILE);

            wrapper.StandardOutputDataReceived += (s, taskOwner, data) => Logger.TaskLogInformation(taskOwner, data);
            wrapper.StandardErrorDataReceived += (s, taskOwner, data) => Logger.TaskLogDebug(taskOwner, data);
            await wrapper.Start(pointer);

            Logger.LogInformation("Completed OWASP ZAP scanner runtime execution in {0}", wrapper.ExecutionTime);
        }
    }
}