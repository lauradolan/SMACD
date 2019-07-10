using System;
using System.Diagnostics;
using System.Threading;
using CommandLine;
using Crayon;
using Microsoft.Extensions.Logging;
using Serilog;
using SMACD.CLITool.Verbs;
using SMACD.Shared;

namespace SMACD.CLITool
{
    internal class Program
    {
        private static ILogger<Program> Logger { get; } = Extensions.LogFactory.CreateLogger<Program>();

        private static void Main(string[] args)
        {
            if (Debugger.IsAttached)
            {
                Console.Write(Output.Underline().Green().Text("Enter arguments:") + " ");
                var strArgs = "";
                while (string.IsNullOrEmpty(strArgs))
                    strArgs = Console.ReadLine().Trim();
                args = strArgs.Split(' ');
            }

            Parser.Default.ParseArguments<GenerateVerb, ReportVerb, ScanVerb, ShowVerb, SnoopVerb, ValidateVerb>(args)
                .WithParsed<GenerateVerb>(RunVerbLifecycle)
                .WithParsed<ReportVerb>(RunVerbLifecycle)
                .WithParsed<ScanVerb>(RunVerbLifecycle)
                .WithParsed<ShowVerb>(RunVerbLifecycle)
                .WithParsed<SnoopVerb>(RunVerbLifecycle)
                .WithParsed<ValidateVerb>(RunVerbLifecycle);
        }

        private static void RunVerbLifecycle(VerbBase verb)
        {
            var currentTimeTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} ";
            var template = "[{Level:u3}<{TaskId}>@{SourceContext}] {Message:lj} {NewLine}{Exception}";
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .Enrich.WithDemystifiedStackTraces()
                .Enrich.WithThreadId()
                .Enrich.WithThreadName()
                .Enrich.With<TaskIdEnricher>()
                .MinimumLevel.Is(verb.LogLevel)
                .WriteTo.Console(outputTemplate: template)
                .WriteTo.File("smacd.log", outputTemplate: currentTimeTemplate + template)
                .CreateLogger();
            Extensions.LogFactory.AddSerilog();

            if (!verb.Silent)
                TerminalEffects.DrawLogoBanner();

            var runningTask = verb.Execute();
            if (runningTask != null)
            {
                while (!runningTask.IsCompleted)
                    Thread.Sleep(100);

                if (runningTask.Exception != null)
                {
                    if (runningTask.Exception is AggregateException)
                        foreach (var item in runningTask.Exception.InnerExceptions)
                            Logger.LogCritical(item, "Error running task");
                    else
                        Logger.LogCritical(runningTask.Exception, "Error running task");
                }

                Logger.LogDebug("Application complete");
            }
        }
    }
}