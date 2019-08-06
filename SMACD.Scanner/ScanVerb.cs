using CommandLine;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SMACD.Data;
using SMACD.Data.Resources;
using SMACD.Workspace;
using SMACD.Workspace.Actions;
using SMACD.Workspace.Artifacts;
using SMACD.Workspace.Targets;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace SMACD.Scanner
{
    [Verb("scan", HelpText = "Run scan using plugins specified in a Service Map")]
    public class ScanVerb : VerbBase
    {
        [Option('s', "servicemap", HelpText = "Location of the Service Map", Required = true)]
        public string ServiceMap { get; set; }

        [Option('d', "workingdirectory", HelpText = "Working directory of Workspace")]
        public string WorkingDirectory { get; set; }

        [Option('t', "threshold", HelpText =
            "Threshold of final score out of 100 at which to fail (return -1 exit code)")]
        public int? Threshold { get; set; }

        private static ILogger<ScanVerb> Logger { get; } = WorkspaceToolbox.LogFactory.CreateLogger<ScanVerb>();

        public override async Task Execute()
        {
            // Serialize/Deserialize "rootArtifact"
            // Need to call pre save/load functions to sew up tree pointers

            var artifactRoot = new Artifact() { Name = "_root_" };
            artifactRoot.Root = artifactRoot;

            // --------------------------------------------------------------------------------------------
            if (string.IsNullOrEmpty(WorkingDirectory))
                WorkingDirectory = Path.Combine(Path.GetTempPath(), "wks_workingdir", DateTime.Now.ToUniversalTime().ToString("u").Replace(" ", string.Empty).Replace(':', '-'));
            if (!Directory.Exists(WorkingDirectory))
                Directory.CreateDirectory(WorkingDirectory);

            // Create workspace against Working Directory (artifacts stored in the dat file path provided)
            var workspace = new Workspace.Workspace(artifactRoot);

            // Register Extension Libraries
            workspace.Libraries.RegisterFromDirectory(Path.Combine(
                Path.GetDirectoryName(Assembly.GetEntryAssembly().FullName),
                "plugins"), "SMACD.Plugins.*.dll");

            // Import Service Map
            var serviceMap = ServiceMapFile.GetServiceMap(ServiceMap);

            // Register Targets from Resources
            foreach (var resourceModel in serviceMap.Targets)
            {
                TargetDescriptor descriptor = null;

                if (resourceModel is HttpResourceModel)
                {
                    var http = resourceModel as HttpResourceModel;
                    descriptor = new HttpTarget()
                    {
                        ResourceLocatorAddress = http.Url,
                        ResourceAccessMode = http.Method,
                        Fields = http.Fields,
                        Headers = http.Headers
                    };
                }
                if (resourceModel is SocketPortResourceModel)
                {
                    var socket = resourceModel as SocketPortResourceModel;
                    descriptor = new RawPortTarget()
                    {
                        RemoteHost = socket.Hostname,
                        Port = socket.Port,
                        Protocol = Enum.Parse<ProtocolType>(socket.Protocol)
                    };
                }

                descriptor.TargetId = resourceModel.TargetId;

                //CopyProperties(resourceModel, descriptor);
                workspace.Targets.RegisterTarget(descriptor);
            }

            // Copy original Service Map to Working Directory
            File.Copy(
                ServiceMap,
                Path.Combine(WorkingDirectory, "input.yaml"));

            var results = new List<Task<ActionSpecificReport>>();
            foreach (var feature in serviceMap.Features)
                foreach (var useCase in feature.UseCases)
                    foreach (var abuseCase in useCase.AbuseCases)
                        foreach (var pluginPointer in abuseCase.Actions)
                        {
                            results.Add(workspace.Tasks.Enqueue(new Workspace.Tasks.TaskDescriptor()
                            {
                                ActionId = "producer." + pluginPointer.Action,
                                Options = pluginPointer.Parameters,
                                TargetIds = new List<string>() { pluginPointer.Target.TargetId }
                            }));
                        }

            while (workspace.Tasks.IsRunning)
            {
                System.Threading.Thread.Sleep(500);
            }
            workspace.Reports.ForEach(r =>
            {
                r.GeneratingTask.UnbindLoops();
            });

            var json = JsonConvert.SerializeObject(workspace.Reports);
            var outputFile = Path.Combine(WorkingDirectory,
                "summary_" + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + ".json");
            using (var sw = new StreamWriter(outputFile))
            {
                sw.WriteLine(json);
            }
            Console.WriteLine(JsonConvert.SerializeObject(workspace.Reports, Formatting.Indented));
            //Console.WriteLine(GDS.ASCII.ASCIITree.GetTree(workspace.Reports, "{GetType().Name}"));
            //TreeDump.Dump(workspace.Reports);

            //Console.WriteLine("Average score: {0}", results.Average(r => r.AdjustedScore));
            //Console.WriteLine("Summed score: {0}", results.Sum(r => r.AdjustedScore));
            //Console.WriteLine("Median score: {0}", results.OrderBy(r => r.AdjustedScore).ElementAt(results.Count / 2));

            //Console.WriteLine("Report serialized to {0}", outputFile);

            //if (Threshold.HasValue)
            //{
            //    Logger.LogDebug("Checking threshold");
            //    if (Threshold > results.Average(r => r.AdjustedScore))
            //    {
            //        Logger.LogInformation("Failed threshold test! Expected: {0} / Actual: {1}", Threshold,
            //            results.Average(r => r.AdjustedScore));
            //        Environment.Exit(-1);
            //    }
            //    else
            //    {
            //        Logger.LogDebug("Passed");
            //        Environment.Exit(0);
            //    }
            //}
        }


        private void RunBeforeSave(Artifact artifact)
        {
            artifact.Root = null;
            artifact.Parent = null;
            foreach (var child in artifact.Children)
                RunBeforeSave(child);
        }

        private void RunAfterLoad(Artifact artifactAtGeneration, Artifact root = null, Artifact parent = null)
        {
            if (parent != null)
                artifactAtGeneration.Parent = parent;
            else
                artifactAtGeneration.Root = artifactAtGeneration;

            artifactAtGeneration.Root = root;

            foreach (var child in artifactAtGeneration.Children)
                RunAfterLoad(child, artifactAtGeneration.Root, artifactAtGeneration);
        }
    }

    internal static class ProgramExtensions
    {
        internal static T AddLoadedTagMappings<T>(this T builder) where T : BuilderSkeleton<T>
        {
            builder.WithTagMapping("!http", typeof(SMACD.Data.Resources.HttpResourceModel));
            builder.WithTagMapping("!socket", typeof(SMACD.Data.Resources.SocketPortResourceModel));
            return builder;
        }
    }
}