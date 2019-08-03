using CommandLine;
using Crayon;
using Microsoft.Extensions.Logging;
using SMACD.Workspace;
using SMACD.Workspace.Actions;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SMACD.CLITool.Verbs
{
    [Verb("snoop", HelpText = "Snoop on the tool's awareness of its environment")]
    public class SnoopVerb : VerbBase
    {
        private static ILogger<SnoopVerb> Logger { get; } = WorkspaceToolbox.LogFactory.CreateLogger<SnoopVerb>();

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

            var workspace = new Workspace.Workspace(null);

            Console.WriteLine(Output.BrightBlue("LOADED LIBRARIES:"));
            foreach (var loaded in workspace.Actions.LoadedActionProviders)
            {
                Console.WriteLine("· " + $"{Output.BrightGreen(loaded.Name)} by {Output.Green(loaded.Author)}");
                Console.WriteLine("  " + Output.White().Text(loaded.FileName));
                Console.WriteLine("  " + Output.White().Dim().Text(loaded.Description));
                var pluginInfo = loaded.ActionsProvided.Select(p => Tuple.Create(p.FullActionId, p)).OrderBy(p => p.Item1)
                    .ToList();
                for (var i = 0; i < pluginInfo.Count; i++)
                {
                    Console.Write(i != 0 ? "  └─ " : "  ├─ ");
                    var outputText = "";
                    switch (pluginInfo[i].Item2.Type)
                    {
                        case ActionRoles.Unknown:
                            outputText = pluginInfo[i].Item1;
                            break;

                        case ActionRoles.Producer:
                            outputText = Output.Red().Text(pluginInfo[i].Item1);
                            break;

                        case ActionRoles.Consumer:
                            outputText = Output.Green().Text(pluginInfo[i].Item1);
                            break;

                        case ActionRoles.Decider:
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