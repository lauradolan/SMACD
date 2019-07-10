using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Crayon;
using Microsoft.Extensions.Logging;
using SMACD.Shared;

namespace SMACD.CLITool.Verbs
{
    [Verb("show", HelpText = "Display the content of a given Service Map")]
    public class ShowVerb : VerbBase
    {
        [Option('s', "servicemap", HelpText = "Service Map file", Required = true)]
        public string ServiceMap { get; set; }

        private static ILogger<ShowVerb> Logger { get; } = Extensions.LogFactory.CreateLogger<ShowVerb>();

        private IList<Tuple<string, string>> _loadedExtensions = Workspace.GetLoadedExtensions();

        public override Task Execute()
        {
            Workspace.Instance.CreateEphemeral(ServiceMap);

            var treeRenderer = new TreeRenderer();

            Console.WriteLine(Output.Reversed().White().Text(Path.GetFileName(ServiceMap)));
            foreach (var feature in Workspace.Instance.Features)
                treeRenderer.PrintNode(feature, "", Workspace.Instance.Features.Last() == feature);

            return Task.FromResult(0);
        }
    }
}