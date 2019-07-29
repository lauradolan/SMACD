using CommandLine;
using Crayon;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.Common;
using Serilog;
using SMACD.CLITool.Verbs;
using SMACD.PluginHost;
using SMACD.PluginHost.Resources;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SMACD.CLITool
{
    internal class Program
    {
        private static ILogger<Program> Logger { get; } = Global.LogFactory.CreateLogger<Program>();

        private static void Main(string[] args)
        {
            if (Debugger.IsAttached || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SMACD_DEBUG")))
            {
                Console.Write(Output.Underline().Green().Text("Enter arguments:") + " ");
                var strArgs = "";
                while (string.IsNullOrEmpty(strArgs))
                    strArgs = Console.ReadLine().Trim();
                args = strArgs.Split(' ');
            }

            BindSerializers();

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
            Global.LogFactory.AddSerilog();

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

        private static void BindSerializers()
        {
            Global.SerializeToFile = (serializedObject, skippedFields, filename) =>
            {
                using (var sw = new StreamWriter(filename))
                {
                    sw.WriteLine(Serialize(serializedObject, skippedFields));
                }
            };
            Global.DeserializeFromFile = (filename, type) =>
            {
                using (var sr = new StreamReader(filename))
                {
                    return Deserialize(sr.ReadToEnd(), type);
                }
            };
            Global.SerializeToString = Serialize;
            Global.DeserializeFromString = Deserialize;
        }

        private static string Serialize(object obj, string[] skippedFields)
        {
            return new SerializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .WithTypeInspector(i => new SkipFieldsInspector(i, skippedFields.ToArray()))
                .AddLoadedTagMappings()
                .Build()
                .Serialize(obj);
        }

        private static object Deserialize(string yaml, Type type)
        {
            return new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .AddLoadedTagMappings()
                .Build()
                .Deserialize(yaml, type);
        }
    }

    internal static class ProgramExtensions
    {
        internal static T AddLoadedTagMappings<T>(this T builder) where T : BuilderSkeleton<T>
        {
            ResourceManager.GetKnownResourceHandlers()
                .ForEach(h => builder = builder.WithTagMapping("!" + h.Key, h.Value));
            return builder;
        }
    }
}