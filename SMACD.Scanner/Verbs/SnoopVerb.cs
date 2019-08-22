using CommandLine;
using Crayon;
using Microsoft.Extensions.Logging;
using SMACD.ScanEngine;
using SMACD.SDK.Triggers;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SMACD.Scanner.Verbs
{
    [Verb("snoop", HelpText = "Snoop on the tool's awareness of its environment")]
    public class SnoopVerb : VerbBase
    {
        private static ILogger<SnoopVerb> Logger { get; } = SMACD.ScanEngine.Global.LogFactory.CreateLogger<SnoopVerb>();

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

            Session session = new Session();

            ExtensionToolbox.Instance.LoadExtensionLibrariesFromPath(
                Path.Combine(Directory.GetCurrentDirectory(), "Plugins"),
                "SMACD.Plugins.*.dll");
            Console.WriteLine(Output.BrightBlue("LOADED LIBRARIES:"));
            foreach (ExtensionLibrary loaded in ExtensionToolbox.Instance.ExtensionLibraries)
            {
                Console.WriteLine("· " + $"{Output.BrightGreen(loaded.FileName)}");
                Console.WriteLine("  " + Output.White().Text(loaded.Assembly.FullName));
                Console.WriteLine("  " + Output.Magenta("ACTIONS:"));
                System.Collections.Generic.List<Tuple<string, System.Collections.Generic.KeyValuePair<string, Type>>> actionInfo = loaded.ActionExtensions.Select(p => Tuple.Create(p.Key, p)).OrderBy(p => p.Item1)
                    .ToList();

                for (int i = 0; i < actionInfo.Count; i++)
                {
                    if (actionInfo.Count == 1)
                    {
                        Console.Write("  └─ ");
                    }
                    else
                    {
                        Console.Write(i != 0 ? "  └─ " : "  ├─ ");
                    }

                    Console.WriteLine(Output.BrightGreen(actionInfo[i].Item1) + " runs " + Output.BrightBlue(actionInfo[i].Item2.Value.FullName));
                }

                Console.WriteLine("  " + Output.Magenta("REACTIONS:"));
                System.Collections.Generic.List<Tuple<TriggerDescriptor, System.Collections.Generic.KeyValuePair<TriggerDescriptor, System.Collections.Generic.List<Type>>>> reactionInfo = loaded.ReactionExtensions.Select(p => Tuple.Create(p.Key, p)).OrderBy(p => p.Item1)
                    .ToList();

                for (int i = 0; i < reactionInfo.Count; i++)
                {
                    if (reactionInfo.Count == 1)
                    {
                        Console.Write("  └─ ");
                    }
                    else
                    {
                        Console.Write(i != 0 ? "  └─ " : "  ├─ ");
                    }

                    foreach (Type reaction in reactionInfo[i].Item2.Value)
                    {
                        Console.WriteLine(Output.BrightGreen(reaction.FullName) + " via " + Output.BrightBlue(reactionInfo[i].Item1.ToString()));
                    }
                }
                Console.WriteLine();
            }

            return Task.FromResult(0);
        }
    }
}