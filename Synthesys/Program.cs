using CommandLine;
using Crayon;
using Microsoft.Extensions.Logging;
using Serilog;
using Synthesys.Helpers;
using Synthesys.Verbs;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Synthesys
{
    internal class Program
    {
        public static string ServiceMap { get; set; }
        public static string WorkingDirectory { get; set; }

        private static ILogger Logger { get; } = Global.LogFactory.CreateLogger("CLI");

        private static void Main(string[] args)
        {
            //┏━┓┏┳┓┏━┓┏━╸╺┳┓
            //┃  ┃┃┃┃ ┃┃   ┃┃  Synthesys Vulnerability Scanning Tool
            //┗━┓┃┃┃┣━┫┃   ┃┃  
            //  ┃┃┃┃┃ ┃┃   ┃┃  github.com/anthturner/SMACD
            //┗━┛╹ ╹╹ ╹┗━╸╺┻┛

            if (Debugger.IsAttached || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SMACD_DEBUG")))
            {
                Console.Write(Output.Underline().Green().Text("Enter arguments:") + " ");
                string strArgs = "";
                while (string.IsNullOrEmpty(strArgs))
                {
                    strArgs = Console.ReadLine().Trim();
                }

                args = strArgs.Split(' ');
            }

            Parser.Default.ParseArguments<GenerateVerb, ScanVerb, SnoopVerb>(args)
                .WithParsed<GenerateVerb>(RunVerbLifecycle)
                .WithParsed<ScanVerb>(RunVerbLifecycle)
                .WithParsed<SnoopVerb>(RunVerbLifecycle);
        }

        private static void RunVerbLifecycle(VerbBase verb)
        {
            string currentTimeTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} ";
            string template = "[{Level:u3}<{TaskId}>@{SourceContext}] {Message:lj} {NewLine}{Exception}";
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
            Global.LogFactory.AddSerilog(Log.Logger);
            SDK.Global.LogFactory.AddSerilog(Log.Logger);
            Tasks.Global.LogFactory.AddSerilog(Log.Logger);

            if (!verb.Silent)
            {
                // -- Draw logo banner --
                Console.OutputEncoding = Encoding.UTF8;
                Branding.DrawBanner();
            }

            System.Threading.Tasks.Task runningTask = verb.Execute();
            if (runningTask != null)
            {
                while (!runningTask.IsCompleted)
                {
                    Thread.Sleep(100);
                }

                if (runningTask.Exception != null)
                {
                    if (runningTask.Exception is AggregateException)
                    {
                        foreach (Exception item in runningTask.Exception.InnerExceptions)
                        {
                            Logger.LogCritical(item, "Error running task");
                        }
                    }
                    else
                    {
                        Logger.LogCritical(runningTask.Exception, "Error running task");
                    }
                }

                Logger.LogDebug("Application complete");
            }
        }
    }
}