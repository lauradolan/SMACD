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
        public string ServiceMapFile { get; set; }

        [Option('n', "session", HelpText = "Location of Session file to generate. If it exists, this scan will be appended to the existing Session, otherwise it will be created")]
        public string SessionFile { get; set; }

        [Option('t', "threshold", HelpText =
            "Threshold of final score out of 100 at which to fail (return -1 exit code)")]
        public int? Threshold { get; set; }

        [Option('f', "feature", HelpText = "Limit scan to a single feature")]
        public string Feature { get; set; }

        [Option('u', "usecase", HelpText = "Limit scan to a single use case (as <feature>//<usecase>)")]
        public string UseCase { get; set; }

        [Option('k', "limitknown", HelpText = "Limit scan to only nodes populated from the Resources section of the Service Map")]
        public bool LimitKnown { get; set; }

        private static ILogger<ScanVerb> Logger { get; } = Global.LogFactory.CreateLogger<ScanVerb>();

        public override Task Execute()
        {
            Logger.LogDebug("Starting ExtensionLibrary search");
            ExtensionToolbox.Instance.LoadExtensionLibrariesFromPath(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugins"),
                "Synthesys.Plugins.*.dll");

            // --------------------------------------------------------------------------------------------

            Session session = null;
            if (!File.Exists(ServiceMapFile))
            {
                Logger.LogCritical("Service map does not exist at {0}", ServiceMapFile);
                Environment.Exit(-2);
            }

            if (!string.IsNullOrEmpty(SessionFile) && File.Exists(SessionFile))
            {
                Logger.LogDebug("Session file exists, opening {0}", SessionFile);
                using (FileStream stream = new FileStream(SessionFile, FileMode.Open, FileAccess.Read))
                {
                    session = Session.Import(stream);
                }
            }
            else
            {
                SessionFile = Path.GetTempPath() + "synthesys_" + Helpers.JargonGenerator.GenerateVerbAdjNounJargon(false).Replace(' ', '-');
                Logger.LogDebug("Creating new Session at {0}", SessionFile);
                session = new Session
                {
                    ServiceMapYaml = File.ReadAllText(ServiceMapFile)
                };
            }

            // Don't fire events during data load
            session.Artifacts.SuppressEventFiring = true;

            // Import Service Map
            ServiceMapFile serviceMap = SMACD.Data.ServiceMapFile.GetServiceMap(ServiceMapFile);

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

            // Queue all tasks if constraints are not specified
            if (string.IsNullOrEmpty(Feature) && string.IsNullOrEmpty(UseCase))
            {
                foreach (FeatureModel feature in serviceMap.Features)
                {
                    foreach (UseCaseModel useCase in feature.UseCases)
                    {
                        foreach (AbuseCaseModel abuseCase in useCase.AbuseCases)
                        {
                            var projectPtr = new ProjectPointer()
                            {
                                Feature = feature,
                                UseCase = useCase,
                                AbuseCase = abuseCase
                            };
                            foreach (ActionPointerModel pluginPointer in abuseCase.Actions)
                            {
                                generatedTasks.Add(QueueTasksFrom(session, serviceMap, projectPtr, pluginPointer));
                            }
                        }
                    }
                }
            }
            else if (!string.IsNullOrEmpty(Feature)) // Queue all tasks under a certain feature
            {
                var targetFeature = serviceMap.Features.FirstOrDefault(f => f.Name.Equals(Feature, StringComparison.OrdinalIgnoreCase));
                if (targetFeature == null)
                {
                    Logger.LogCritical("Feature '{0}' not found.", Feature);
                    Environment.Exit(-3);
                }

                Logger.LogInformation("Queueing by feature '{0}'", targetFeature.Name);
                foreach (var useCase in targetFeature.UseCases)
                {
                    foreach (var abuseCase in useCase.AbuseCases)
                    {
                        var projectPtr = new ProjectPointer()
                        {
                            Feature = targetFeature,
                            UseCase = useCase,
                            AbuseCase = abuseCase
                        };
                        foreach (ActionPointerModel pluginPointer in abuseCase.Actions)
                        {
                            generatedTasks.Add(QueueTasksFrom(session, serviceMap, projectPtr, pluginPointer));
                        }
                    }
                }
            }
            else if (!string.IsNullOrEmpty(UseCase)) // Queue all tasks under a certain feature//usecase
            {
                if (!UseCase.Contains("//"))
                {
                    Logger.LogCritical("UseCase must be specified in the format '{0}//{1}'", "<feature>", "<usecase>");
                    Environment.Exit(-4);
                }
                var targetFeature = serviceMap.Features.FirstOrDefault(f => f.Name.Equals(UseCase.Split("//")[0], StringComparison.OrdinalIgnoreCase));
                if (targetFeature == null)
                {
                    Logger.LogCritical("Feature '{0}' not found.", UseCase.Split("//")[0]);
                    Environment.Exit(-5);
                }
                var targetUseCase = targetFeature.UseCases.FirstOrDefault(u => u.Name.Equals(UseCase.Split("//")[1], StringComparison.OrdinalIgnoreCase));
                if (targetUseCase == null)
                {
                    Logger.LogCritical("Use Case '{0}' not found.", UseCase.Split("//")[1]);
                    Environment.Exit(-6);
                }

                Logger.LogInformation("Queueing by use case '{0} under feature '{1}'", targetUseCase.Name, targetFeature.Name);
                foreach (var abuseCase in targetUseCase.AbuseCases)
                {
                    var projectPtr = new ProjectPointer()
                    {
                        Feature = targetFeature,
                        UseCase = targetUseCase,
                        AbuseCase = abuseCase
                    };
                    foreach (ActionPointerModel pluginPointer in abuseCase.Actions)
                    {
                        generatedTasks.Add(QueueTasksFrom(session, serviceMap, projectPtr, pluginPointer));
                    }
                }
            }

            if (LimitKnown)
            {
                Logger.LogWarning("Running in constrained mode -- Extensions will not add additional nodes to AppTree");
                session.Artifacts.LockTreeNodes = true;
            }

            while (session.Tasks.IsRunning)
            {
                Thread.Sleep(500);
            }

            IEnumerable<ExtensionReport> results = generatedTasks.SelectMany(t => t.Result.Select(r => r.FinalizeReport()));
            session.Reports.AddRange(results);

            using (FileStream stream = new FileStream(SessionFile, FileMode.OpenOrCreate,
                FileAccess.Write))
            {
                session.Export(stream);
            }

            if (!Silent)
            {
                Logger.LogInformation("Report serialized to {0}", SessionFile);
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

        private Task<List<ExtensionReport>> QueueTasksFrom(Session session, ServiceMapFile serviceMap, ProjectPointer projectPointer, ActionPointerModel pluginPointer)
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

            return session.Tasks.Enqueue(pluginPointer.Action,
                artifact,
                pluginPointer.Options,
                projectPointer
            );
        }
    }
}