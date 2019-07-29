using CommandLine;
using Crayon;
using Microsoft.Extensions.Logging;
using SMACD.PluginHost;
using SMACD.PluginHost.Extensions;
using SMACD.PluginHost.Plugins;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SMACD.CLITool.Verbs
{
    [Verb("snoop", HelpText = "Snoop on the tool's awareness of its environment")]
    public class SnoopVerb : VerbBase
    {
        private static ILogger<SnoopVerb> Logger { get; } = Global.LogFactory.CreateLogger<SnoopVerb>();

        public override Task Execute()
        {
            Logger.LogDebug("Starting snoop on environment configuration");

            Console.WriteLine(Environment.NewLine);
            Console.WriteLine(Output.BrightBlue("HOST ENVIRONMENT:"));
            Console.Write(Output.BrightWhite("SMACD CLI Tool running on "));
            Console.Write(Output.BrightGreen(Environment.MachineName));
            Console.Write(" running ");
            Console.Write(Output.BrightMagenta(Environment.OSVersion.ToString()));

            Console.WriteLine(Environment.NewLine);

            Console.WriteLine(Output.BrightBlue("LOADED LIBRARIES:"));
            foreach (var loaded in PluginLibrary.LoadedLibraries)
            {
                Console.WriteLine("· " + $"{Output.BrightGreen(loaded.Name)} by {Output.Green(loaded.Author)}");
                Console.WriteLine("  " + Output.White().Text(loaded.FileName));
                Console.WriteLine("  " + Output.White().Dim().Text(loaded.Description));
                var pluginInfo = loaded.PluginsProvided.Select(p => Tuple.Create(p.Identifier, p)).OrderBy(p => p.Item1)
                    .ToList();
                for (var i = 0; i < pluginInfo.Count; i++)
                {
                    Console.Write(i != 0 ? "  └─ " : "  ├─ ");
                    var outputText = "";
                    switch (pluginInfo[i].Item2.PluginType)
                    {
                        case PluginTypes.Unknown:
                            outputText = pluginInfo[i].Item1;
                            break;

                        case PluginTypes.AttackTool:
                            outputText = Output.Red().Text(pluginInfo[i].Item1);
                            break;

                        case PluginTypes.Scorer:
                            outputText = Output.Green().Text(pluginInfo[i].Item1);
                            break;

                        case PluginTypes.Decision:
                            outputText = Output.Yellow().Text(pluginInfo[i].Item1);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    Console.WriteLine(outputText);
                }
            }

            return Task.FromResult(0);
        }
    }
}