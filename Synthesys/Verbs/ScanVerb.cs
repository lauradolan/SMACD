using CommandLine;
using Microsoft.Extensions.Logging;
using SMACD.AppTree;
using SMACD.Data;
using SMACD.Data.Resources;
using Synthesys.SDK;
using Synthesys.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Synthesys.Verbs
{
    [Verb("scan", HelpText = "Run scan using plugins specified in a Service Map")]
    public class ScanVerb : VerbBase
    {

        [Option('s', "servicemap", HelpText = "Location of the Service Map", Required = true)]
        public string ServiceMap { get; set; }

        [Option('o', "output", HelpText = "Location of Session file output")]
        public string Output { get; set; } = Path.GetTempFileName();

        [Option('t', "threshold", HelpText =
            "Threshold of final score out of 100 at which to fail (return -1 exit code)")]
        public int? Threshold { get; set; }

        private static ILogger<ScanVerb> Logger { get; } = Global.LogFactory.CreateLogger<ScanVerb>();

        public override Task Execute()
        {
            Logger.LogDebug("Starting ExtensionLibrary search");
            ExtensionToolbox.Instance.LoadExtensionLibrariesFromPath(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins"),
                "Synthesys.Plugins.*.dll");

            // --------------------------------------------------------------------------------------------

            if (!File.Exists(ServiceMap))
            {
                Logger.LogCritical("Service map does not exist at {0}", ServiceMap);
                Environment.Exit(-2);
            }

            Session session = null;
            if (File.Exists(Output))
            {
                Logger.LogDebug("Session file exists, opening {0}", Output);
                using (FileStream stream = new FileStream(Output, FileMode.Open, FileAccess.Read))
                {
                    session = Session.Import(stream);
                }
            }
            else
            {
                Logger.LogDebug("Creating new Session at {0}", Output);
                session = new Session
                {
                    ServiceMapYaml = File.ReadAllText(ServiceMap)
                };
            }

            // Don't fire events during data load
            session.Artifacts.SuppressEventFiring = true;

            // Import Service Map
            ServiceMapFile serviceMap = ServiceMapFile.GetServiceMap(ServiceMap);

            // Register Targets from Resources
            foreach (TargetModel resourceModel in serviceMap.Targets)
            {
                session.RegisterTarget(resourceModel);
            }

            // Restore Artifact event firing
            session.Artifacts.SuppressEventFiring = false;

            // Apply Service Map to TaskToolbox to be included in some Extensions
            session.Tasks.ServiceMap = serviceMap;

            List<Task<List<ExtensionReport>>> generatedTasks = new List<Task<List<ExtensionReport>>>();
            foreach (FeatureModel feature in serviceMap.Features)
            {
                foreach (UseCaseModel useCase in feature.UseCases)
                {
                    foreach (AbuseCaseModel abuseCase in useCase.AbuseCases)
                    {
                        foreach (ActionPointerModel pluginPointer in abuseCase.Actions)
                        {
                            TargetModel target = serviceMap.Targets.FirstOrDefault(t => t.TargetId == pluginPointer.Target);

                            AppTreeNode artifact = null;
                            if (target is HttpTargetModel)
                            {
                                Uri uri = new Uri(((HttpTargetModel)target).Url);
                                artifact = session.Artifacts[uri.Host][uri.Port];
                            }
                            else if (target is SocketPortTargetModel)
                            {
                                artifact = session.Artifacts
                                        [((SocketPortTargetModel)target).Hostname]
                                    [((SocketPortTargetModel)target).Port];
                            }

                            generatedTasks.Add(session.Tasks.Enqueue(pluginPointer.Action,
                                artifact,
                                pluginPointer.Options,
                                new ProjectPointer
                                {
                                    Feature = feature,
                                    UseCase = useCase,
                                    AbuseCase = abuseCase
                                }
                            ));
                        }
                    }
                }
            }

            while (session.Tasks.IsRunning)
            {
                Thread.Sleep(500);
            }

            IEnumerable<ExtensionReport> results = generatedTasks.SelectMany(t => t.Result.Select(r => r.FinalizeReport()));
            session.Reports.AddRange(results);

            using (FileStream stream = new FileStream(Output, FileMode.OpenOrCreate,
                FileAccess.Write))
            {
                session.Export(stream);
            }

            if (!Silent)
            {
                Logger.LogInformation("Report serialized to {0}", Output);
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
    }
}