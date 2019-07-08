using CommandLine;
using Crayon;
using Microsoft.Extensions.Logging;
using Serilog;
using SMACD.CLITool.Verbs;
using System;
using System.Threading;

namespace SMACD.CLITool
{
    internal class Program
    {
        private static ILogger<Program> Logger { get; set; } = Shared.Extensions.LogFactory.CreateLogger<Program>();

        private static void Main(string[] args)
        {
            if (System.Diagnostics.Debugger.IsAttached)
            {
                Console.Write(Output.Underline().Green().Text("Enter arguments:") + " ");
                var strArgs = "";
                while (string.IsNullOrEmpty(strArgs))
                    strArgs = Console.ReadLine().Trim();
                args = strArgs.Split(' ');
            }

            CommandLine.Parser.Default.ParseArguments<ScanVerb, ReportVerb>(args)
                .WithParsed<ScanVerb>(RunVerbLifecycle)
                .WithParsed<ReportVerb>(RunVerbLifecycle);
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
            Shared.Extensions.LogFactory.AddSerilog();

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
                    {
                        foreach (var item in ((AggregateException)runningTask.Exception).InnerExceptions)
                            Logger.LogCritical(item, "Error running task");
                    }
                    else
                        Logger.LogCritical(runningTask.Exception, "Error running task");
                }

                Logger.LogDebug("Application complete");
            }
        }
    }
}