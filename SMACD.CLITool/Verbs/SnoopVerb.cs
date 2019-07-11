using System;
using System.Threading.Tasks;
using CommandLine;
using Crayon;
using Microsoft.Extensions.Logging;
using SMACD.Shared;

namespace SMACD.CLITool.Verbs
{
    [Verb("snoop", HelpText = "Snoop on the tool's awareness of its environment")]
    public class SnoopVerb : VerbBase
    {
        private static ILogger<SnoopVerb> Logger { get; } = Workspace.LogFactory.CreateLogger<SnoopVerb>();

        public override Task Execute()
        {
            var extensions = Workspace.GetLoadedExtensions();
            Logger.LogInformation("Starting snoop on environment configuration");

            Console.WriteLine(Environment.NewLine);
            Console.WriteLine(Output.BrightBlue("HOST ENVIRONMENT:"));
            Console.Write(Output.BrightWhite("SMACD CLI Tool running on "));
            Console.Write(Output.BrightGreen(Environment.MachineName));
            Console.Write(" running ");
            Console.Write(Output.BrightMagenta(Environment.OSVersion.ToString()));

            Console.WriteLine(Environment.NewLine);

            Console.WriteLine(Output.BrightBlue("AVAILABLE MODULES:"));
            foreach (var item in extensions)
                Console.WriteLine(
                    Output.BrightWhite("· " + $"[{Output.BrightGreen(item.Item1)}]".PadRight(24) + item.Item2));

            return Task.FromResult(0);
        }
    }
}