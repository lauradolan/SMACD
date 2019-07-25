using CommandLine;
using Crayon;
using Microsoft.Extensions.Logging;
using SMACD.Data;
using SMACD.PluginHost;
using SMACD.PluginHost.Plugins;
using SMACD.PluginHost.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SMACD.CLITool.Verbs
{
    [Verb("validate", HelpText = "Validate the content of a given Service Map")]
    public class ValidateVerb : VerbBase
    {
        private readonly IDictionary<string, PluginDescription> _loadedExtensions = PluginLibrary.PluginsAvailable;

        private int _tasksGenerated;

        [Option('s', "servicemap", HelpText = "Service Map file", Required = true)]
        public string ServiceMap { get; set; }

        private static ILogger<ValidateVerb> Logger { get; } = Global.LogFactory.CreateLogger<ValidateVerb>();

        public override Task Execute()
        {
            var serviceMap = ServiceMapFile.GetServiceMap(ServiceMap);

            var treeRenderer = new TreeRenderer();
            treeRenderer.AfterPluginPointerDrawn += (indent, isLast, pluginPointer) =>
            {
                _tasksGenerated++;
                var newIndent = treeRenderer.WriteExecutedTest("Supports Plugin type?", () => _loadedExtensions.ContainsKey(pluginPointer.Plugin), indent, isLast);
                if (!_loadedExtensions.ContainsKey(pluginPointer.Plugin))
                {
                    var tests = new[]
                    {
                        Tuple.Create("ResourceModel Map contains ResourceModel?", new Func<bool?>(() =>
                        {
                            if (pluginPointer.Resource == null) return null;
                            return ResourceManager.Instance.ContainsId(pluginPointer.Resource.ResourceId);
                        }))
                    };
                    foreach (var test in tests)
                        treeRenderer.WriteExecutedTest(test.Item1, test.Item2, newIndent, tests.Last().Equals(test));
                }
            };

            Console.WriteLine(Output.Reversed().White().Text(Path.GetFileName(ServiceMap)));
            foreach (var feature in serviceMap.Features)
                treeRenderer.PrintNode(feature, "", serviceMap.Features.Last() == feature);

            Logger.LogInformation(
                "Validation of {0} complete! Tests: {1} passed, {2} failed. Service Map scan will generate {3} tasks.",
                ServiceMap, treeRenderer.TestsPassed, treeRenderer.TestsFailed, _tasksGenerated);

            return Task.FromResult(0);
        }
    }
}