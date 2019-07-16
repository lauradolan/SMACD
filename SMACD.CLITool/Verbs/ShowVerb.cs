using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Crayon;
using Microsoft.Extensions.Logging;
using SMACD.ScannerEngine;

namespace SMACD.CLITool.Verbs
{
    [Verb("show", HelpText = "Display the content of a given Service Map")]
    public class ShowVerb : VerbBase
    {
        private IList<Tuple<string, string>> _loadedExtensions = Global.GetLoadedExtensions();

        [Option('s', "servicemap", HelpText = "Service Map file", Required = true)]
        public string ServiceMap { get; set; }

        private static ILogger<ShowVerb> Logger { get; } = Global.LogFactory.CreateLogger<ShowVerb>();

        public override Task Execute()
        {
            var serviceMap = Global.GetServiceMap(ServiceMap);
            var treeRenderer = new TreeRenderer();

            Console.WriteLine(Output.Reversed().White().Text(Path.GetFileName(ServiceMap)));
            foreach (var feature in serviceMap.Features)
                treeRenderer.PrintNode(feature, "", serviceMap.Features.Last() == feature);

            return Task.FromResult(0);
        }
    }
}