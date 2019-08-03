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
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SMACD.CLITool.Verbs
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
            //WorkspaceToolbox.Loader = new WorkspaceToolbox.LoadArtifactContainerDelegate((artifactsFile) =>
            //{
            //    //var artifactsFile = Path.Combine(WorkingDirectory, "artifacts.dat");
            //    if (!File.Exists(artifactsFile))
            //    {
            //        Logger.LogWarning("Artifact repository not found, starting over!");
            //        return new ArtifactRootContainer();
            //    }
            //    else
            //        return JsonConvert.DeserializeObject<ArtifactRootContainer>(File.ReadAllText(artifactsFile));
            //});
            //WorkspaceToolbox.Saver = new WorkspaceToolbox.SaveArtifactContainerDelegate((rootContainer) =>
            //{
            //    var artifactsFile = Path.Combine(WorkingDirectory, "artifacts.dat");
            //    File.WriteAllText(artifactsFile, JsonConvert.SerializeObject(rootContainer));
            //});

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

            // Register Actions from Plugins
            workspace.Actions.RegisterActionsFromDirectory(Path.Combine(
                Path.GetDirectoryName(Assembly.GetEntryAssembly().FullName),
                "plugins"), "SMACD.Plugins.Dummy.dll"); // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

            // Import Service Map
            var serviceMap = ServiceMapFile.GetServiceMap(ServiceMap);

            // Register Targets from Resources
            foreach (var resourceModel in serviceMap.Resources)
            {
                TargetDescriptor descriptor = null;

                if (resourceModel is HttpResourceModel)
                    descriptor = new HttpTarget();
                if (resourceModel is SocketPortResourceModel)
                    descriptor = new RawPortTarget();

                descriptor.TargetId = resourceModel.ResourceId;

                CopyProperties(resourceModel, descriptor);
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
                        foreach (var pluginPointer in abuseCase.PluginPointers)
                        {
                            results.Add(workspace.Tasks.Enqueue(new Workspace.Tasks.TaskDescriptor()
                            {
                                ActionId = "producer." + pluginPointer.Plugin,
                                Options = pluginPointer.Parameters,
                                TargetIds = new List<string>() { pluginPointer.Resource.ResourceId }
                            }));
                        }

            while (workspace.Tasks.IsRunning)
            {
                System.Threading.Thread.Sleep(500);
            }

            foreach (var result in workspace.Reports)
            {
                //Console.Write(Output.BrightWhite("Plugin: "));
                //PluginLibrary.PluginsAvailable[result.Plugin.Identifier].PluginType.WriteTypeColoredText(
                //    PluginLibrary.PluginsAvailable[result.Plugin.Identifier].Identifier + Environment.NewLine);

                TreeDump.Dump(result, result.GeneratingTask.ActionId);
            }

            //var outputFile = Path.Combine(WorkingDirectory,
            //    "summary_" + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + ".json");
            //using (var sw = new StreamWriter(outputFile))
            //{
            //    sw.WriteLine(JsonConvert.SerializeObject(results));
            //}

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

        private static void CopyProperties<TSrc, TDest>(TSrc source, TDest dest)
        {
            var sourceProperties = source.GetType().GetProperties();
            var destProperties = dest.GetType().GetProperties();
            var copyable = sourceProperties.Where(s =>
                destProperties.Any(d => d.Name == s.Name && d.PropertyType == s.PropertyType));
            foreach (var prop in copyable)
            {
                var target = destProperties.FirstOrDefault(p => p.Name == prop.Name);
                var oldValue = target.GetValue(dest);
                var newValue = prop.GetValue(source);
                if (oldValue != null && newValue == null)
                    continue;
                target.SetValue(dest, prop.GetValue(source));
            }
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
            builder.WithTagMapping("!http", typeof(SMACD.Data.Resources.HttpResourceModel));
            builder.WithTagMapping("!socket", typeof(SMACD.Data.Resources.SocketPortResourceModel));
            return builder;
        }
    }
}