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

        [Option('t', "threshold", HelpText =
            "Threshold of final score out of 100 at which to fail (return -1 exit code)")]
        public int? Threshold { get; set; }

        private static ILogger<ValidateVerb> Logger { get; } = Extensions.LogFactory.CreateLogger<ValidateVerb>();

        public override Task Execute()
        {
            var serviceMap = Workspace.GetServiceMap(ServiceMap);

            Logger.LogInformation("Validating that all Resource IDs exist");
            foreach (var feature in serviceMap.Features)
            foreach (var useCase in feature.UseCases)
            foreach (var abuseCase in useCase.AbuseCases)
            foreach (var ptr in abuseCase.PluginPointers)
                if (ptr.Resource != null)
                    if (!ResourceManager.Instance.ContainsPointer(ptr.Resource))
                        Logger.LogError(
                            $"{feature.Name} -> {useCase.Name} -> {abuseCase.Name} -> {ptr.Plugin} : Resource list does *not* contain the resource requested");

            return Task.FromResult(0);
        }
    }
}