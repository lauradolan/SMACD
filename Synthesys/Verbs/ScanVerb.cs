﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.Logging;
using SMACD.Artifacts;
using SMACD.Data;
using SMACD.Data.Resources;
using Synthesys.SDK;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Synthesys.Verbs
{
    [Verb("scan", HelpText = "Run scan using plugins specified in a Service Map")]
    public class ScanVerb : VerbBase
    {
        private bool workingDirectoryProvided;

        [Option('s', "servicemap", HelpText = "Location of the Service Map", Required = true)]
        public string ServiceMap { get; set; }

        [Option('d', "workingdirectory", HelpText = "Working directory of Workspace")]
        public string WorkingDirectory { get; set; }

        [Option('t', "threshold", HelpText =
            "Threshold of final score out of 100 at which to fail (return -1 exit code)")]
        public int? Threshold { get; set; }

        private static ILogger<ScanVerb> Logger { get; } = Global.LogFactory.CreateLogger<ScanVerb>();

        public override Task Execute()
        {
            Logger.LogDebug("Starting ExtensionLibrary search");
            ExtensionToolbox.Instance.LoadExtensionLibrariesFromPath(
                Path.Combine(Directory.GetCurrentDirectory(), "Plugins"),
                "Synthesys.Plugins.*.dll");

            // --------------------------------------------------------------------------------------------
            if (string.IsNullOrEmpty(WorkingDirectory))
            {
                workingDirectoryProvided = true;
                WorkingDirectory = Path.Combine(Path.GetTempPath(), "wks_workingdir",
                    DateTime.Now.ToUniversalTime().ToString("u").Replace(" ", string.Empty).Replace(':', '-'));
            }

            Session session = null;
            if (File.Exists(Path.Combine(WorkingDirectory, "session")))
            {
                Logger.LogDebug("Session file exists, opening", WorkingDirectory);
                using (var stream = new FileStream(Path.Combine(WorkingDirectory, "session"), FileMode.Open,
                    FileAccess.Read))
                {
                    session = new Session(stream);
                }
            }
            else
            {
                Logger.LogDebug("Session file not found in Working Directory {0}, creating new Session",
                    WorkingDirectory);
                if (!Directory.Exists(WorkingDirectory))
                    Directory.CreateDirectory(WorkingDirectory);

                session = new Session();
                session.ServiceMapYaml = File.ReadAllText(ServiceMap);
            }

            // Import Service Map
            var serviceMap = ServiceMapFile.GetServiceMap(ServiceMap);

            // Register Targets from Resources
            foreach (var resourceModel in serviceMap.Targets)
                session.RegisterTarget(resourceModel);

            var generatedTasks = new List<Task<ExtensionReport>>();
            foreach (var feature in serviceMap.Features)
            foreach (var useCase in feature.UseCases)
            foreach (var abuseCase in useCase.AbuseCases)
            foreach (var pluginPointer in abuseCase.Actions)
            {
                var target = serviceMap.Targets.FirstOrDefault(t => t.TargetId == pluginPointer.Target);

                Artifact artifact = null;
                if (target is HttpTargetModel)
                {
                    var uri = new Uri(((HttpTargetModel) target).Url);
                    artifact = session.Artifacts[uri.Host][uri.Port];
                }
                else if (target is SocketPortTargetModel)
                {
                    artifact = session.Artifacts
                            [((SocketPortTargetModel) target).Hostname]
                        [((SocketPortTargetModel) target).Port];
                }

                generatedTasks.Add(session.Tasks.Enqueue(new TaskDescriptor
                {
                    ActionId = pluginPointer.Action,
                    Options = pluginPointer.Options,
                    ArtifactRoot = artifact,
                    ProjectPointer = new ProjectPointer
                    {
                        Feature = feature,
                        UseCase = useCase,
                        AbuseCase = abuseCase
                    }
                }));
            }

            while (session.Tasks.IsRunning) Thread.Sleep(500);

            var results = generatedTasks.Select(t => t.Result.FinalizeReport());
            session.Reports.AddRange(results);

            using (var stream = new FileStream(Path.Combine(WorkingDirectory, "session"), FileMode.OpenOrCreate,
                FileAccess.Write))
            {
                session.Export(stream);
            }

            if (!Silent || workingDirectoryProvided)
                Logger.LogInformation("Report serialized to {0}", Path.Combine(WorkingDirectory, "session"));

            if (!Silent)
            {
                Console.WriteLine("Average score: {0}", results.Average(r => r.AdjustedScore));
                Console.WriteLine("Summed score: {0}", results.Sum(r => r.AdjustedScore));
                Console.WriteLine("Median score: {0}",
                    results.OrderBy(r => r.AdjustedScore).ElementAt(results.Count() / 2).AdjustedScore);
            }

            if (Threshold.HasValue)
            {
                Logger.LogDebug("Checking threshold");
                if (Threshold > results.Average(r => r.AdjustedScore))
                {
                    Logger.LogInformation("Failed threshold test! Expected: {0} / Actual: {1}", Threshold,
                        results.Average(r => r.AdjustedScore));
                    Console.WriteLine(results.Average(r => r.AdjustedScore));
                    Environment.Exit(-1);
                }
                else
                {
                    Logger.LogDebug("Passed");
                    Environment.Exit(0);
                }
            }

            return Task.FromResult(0);
        }

        private static string Serialize<T>(T obj)
        {
            return new SerializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .WithTagMapping("!http", typeof(HttpTargetModel))
                .WithTagMapping("!raw", typeof(SocketPortTargetModel))
                .Build()
                .Serialize(obj);
        }

        private static T Deserialize<T>(string yaml)
        {
            return new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .WithTagMapping("!http", typeof(HttpTargetModel))
                .WithTagMapping("!raw", typeof(SocketPortTargetModel))
                .Build()
                .Deserialize<T>(yaml);
        }
    }
}