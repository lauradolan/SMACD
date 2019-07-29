using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Crayon;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SMACD.Data;
using SMACD.Data.Resources;
using SMACD.PluginHost;
using SMACD.PluginHost.Extensions;
using SMACD.PluginHost.Plugins;
using SMACD.PluginHost.Reports;
using SMACD.PluginHost.Resources;

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

        private static ILogger<ScanVerb> Logger { get; } = Global.LogFactory.CreateLogger<ScanVerb>();

        public override async Task Execute()
        {
            // Hotpatch workspace storage
            if (WorkingDirectory == null)
                WorkingDirectory = Path.Combine(Path.GetTempPath(), "SMACD", RandomExtensions.RandomName());
            SMACD.PluginHost.WorkingDirectory.WorkingDirectoryBaseLocation = WorkingDirectory;
            if (!Directory.Exists(WorkingDirectory))
                Directory.CreateDirectory(WorkingDirectory);

            var serviceMap = ServiceMapFile.GetServiceMap(ServiceMap);
            foreach (var resourceModel in serviceMap.Resources)
            {
                Resource dest = null;
                if (resourceModel is HttpResourceModel)
                    dest = new HttpResource();
                if (resourceModel is SocketPortResourceModel)
                    dest = new SocketPortResource();
                CopyProperties(resourceModel, dest);
                ResourceManager.Instance.Register(dest);
            }
            File.Copy(
                ServiceMap,
                Path.Combine(WorkingDirectory, "input.yaml"));

            Logger.LogDebug("{0} Libraries Loaded", PluginLibrary.LoadedLibraries.Count);

            var attackToolTasks = new List<Task<ScoredResult>>();
            foreach (var feature in serviceMap.Features)
            foreach (var useCase in feature.UseCases)
            foreach (var abuseCase in useCase.AbuseCases)
            foreach (var pluginPointer in abuseCase.PluginPointers)
            {
                var resources = new List<Resource>
                    {ResourceManager.Instance.GetById(pluginPointer.Resource.ResourceId)};
                attackToolTasks.Add(
                    TaskManager.Instance.Enqueue(new PluginSummary
                    {
                        Identifier = $"attack.{pluginPointer.Plugin}",
                        Options = pluginPointer.PluginParameters,
                        ResourceIds = resources.Select(r => r.ResourceId).ToList()
                    }));
            }

            while (TaskManager.Instance.IsCurrentlyRunning)
            {
                System.Threading.Thread.Sleep(500);
            }

            ResourceManager.Instance.Clear();
            var results = attackToolTasks.Select(t => t.Result).ToList();
            foreach (var result in results)
            {
                //Console.Write(Output.BrightWhite("Plugin: "));
                //PluginLibrary.PluginsAvailable[result.Plugin.Identifier].PluginType.WriteTypeColoredText(
                //    PluginLibrary.PluginsAvailable[result.Plugin.Identifier].Identifier + Environment.NewLine);

                TreeDump.Dump(result, PluginLibrary.PluginsAvailable[result.Plugin.Identifier].PluginType
                    .GetTypeColoredText(result.Plugin.Identifier));
            }

            var outputFile = Path.Combine(WorkingDirectory,
                "summary_" + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + ".json");
            using (var sw = new StreamWriter(outputFile))
            {
                sw.WriteLine(JsonConvert.SerializeObject(results));
            }

            Console.WriteLine("Average score: {0}", results.Average(r => r.AdjustedScore));
            Console.WriteLine("Summed score: {0}", results.Sum(r => r.AdjustedScore));
            Console.WriteLine("Median score: {0}", results.OrderBy(r => r.AdjustedScore).ElementAt(results.Count / 2));

            Console.WriteLine("Report serialized to {0}", outputFile);

            if (Threshold.HasValue)
            {
                Logger.LogDebug("Checking threshold");
                if (Threshold > results.Average(r => r.AdjustedScore))
                {
                    Logger.LogInformation("Failed threshold test! Expected: {0} / Actual: {1}", Threshold,
                        results.Average(r => r.AdjustedScore));
                    Environment.Exit(-1);
                }
                else
                {
                    Logger.LogDebug("Passed");
                    Environment.Exit(0);
                }
            }
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
    }
}