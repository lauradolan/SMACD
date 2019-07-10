using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.Logging;
using SMACD.Shared;
using SMACD.Shared.WorkspaceManagers;

namespace SMACD.CLITool.Verbs
{
    [Verb("validate", HelpText = "Validate the content of a given Service Map")]
    public class ValidateVerb : VerbBase
    {
        [Option('s', "servicemap", HelpText = "Service Map file", Required = true)]
        public string ServiceMap { get; set; }

        private static ILogger<ValidateVerb> Logger { get; } = Extensions.LogFactory.CreateLogger<ValidateVerb>();

        public override Task Execute()
        {
            var issues = 0;
            Workspace.Instance.CreateEphemeral(ServiceMap);

            Logger.LogInformation("Grabbing list of loaded extensions");
            var extensions = Workspace.GetLoadedExtensions();

            Logger.LogInformation("Validating that all Resource IDs exist");
            Workspace.Instance.IteratePluginPointers((feature, useCase, abuseCase, pluginPointer) =>
            {
                var plugin = extensions.FirstOrDefault(e => e.Item1.Equals(pluginPointer.Plugin));
                if (plugin == null)
                {
                    Logger.LogError(
                        $"{feature.Name} -> {useCase.Name} -> {abuseCase.Name} -> {pluginPointer.Plugin} : Requested plugin name is not loaded");
                    issues++;
                }
                else if (!Workspace.Instance.Validate(pluginPointer))
                {
                    Logger.LogError(
                        $"{feature.Name} -> {useCase.Name} -> {abuseCase.Name} -> {pluginPointer.Plugin} : Plugin pointer is not valid");
                    issues++;
                }

                if (pluginPointer.Resource == null) return;
                if (!ResourceManager.Instance.ContainsPointer(pluginPointer.Resource))
                {
                    Logger.LogError(
                        $"{feature.Name} -> {useCase.Name} -> {abuseCase.Name} -> {pluginPointer.Plugin} : Resource list does *not* contain the resource requested");
                    issues++;
                }
            });

            Logger.LogInformation("Validation of {0} complete! Found {0} issues", ServiceMap, issues);

            return Task.FromResult(0);
        }
    }
}