using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Crayon;
using Microsoft.Extensions.Logging;
using SMACD.Shared;
using SMACD.Shared.WorkspaceManagers;

namespace SMACD.CLITool.Verbs
{
    [Verb("validate", HelpText = "Validate the content of a given Service Map")]
    public class ValidateVerb : VerbBase
    {
        private readonly IList<Tuple<string, string>> _loadedExtensions = Workspace.GetLoadedExtensions();

        private int _tasksGenerated;

        [Option('s', "servicemap", HelpText = "Service Map file", Required = true)]
        public string ServiceMap { get; set; }

        private static ILogger<ValidateVerb> Logger { get; } = Workspace.LogFactory.CreateLogger<ValidateVerb>();

        public override Task Execute()
        {
            Workspace.Instance.CreateEphemeral(ServiceMap);

            var treeRenderer = new TreeRenderer();
            treeRenderer.AfterPluginPointerDrawn += (indent, isLast, pluginPointer) =>
            {
                _tasksGenerated++;
                var plugin = _loadedExtensions.FirstOrDefault(e => e.Item1.Equals(pluginPointer.Plugin));
                var newIndent =
                    treeRenderer.WriteExecutedTest("Supports Plugin type?", () => plugin != null, indent, isLast);
                if (plugin != null)
                {
                    var tests = new[]
                    {
                        Tuple.Create("Plugin pointer has valid options?",
                            new Func<bool?>(() => Workspace.Instance.Validate(pluginPointer))),
                        Tuple.Create("Resource Map contains Resource?", new Func<bool?>(() =>
                        {
                            if (pluginPointer.Resource == null) return null;
                            return ResourceManager.Instance.ContainsPointer(pluginPointer.Resource);
                        }))
                    };
                    foreach (var test in tests)
                        treeRenderer.WriteExecutedTest(test.Item1, test.Item2, newIndent, tests.Last().Equals(test));
                }
            };

            Console.WriteLine(Output.Reversed().White().Text(Path.GetFileName(ServiceMap)));
            foreach (var feature in Workspace.Instance.Features)
                treeRenderer.PrintNode(feature, "", Workspace.Instance.Features.Last() == feature);

            Logger.LogInformation(
                "Validation of {0} complete! Tests: {1} passed, {2} failed. Service Map scan will generate {3} tasks.",
                ServiceMap, treeRenderer.TestsPassed, treeRenderer.TestsFailed, _tasksGenerated);

            return Task.FromResult(0);
        }
    }
}