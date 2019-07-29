using CommandLine;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SMACD.Data;
using SMACD.Data.Resources;
using SMACD.PluginHost;
using SMACD.PluginHost.Plugins;
using SMACD.PluginHost.Reports;
using SMACD.PluginHost.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SMACD.CLITool.Verbs
{
    [Verb("report", HelpText = "Regenerate report from a working directory")]
    public class ReportVerb : VerbBase
    {
        [Option('d', "workingdir", HelpText = "Working directory", Required = true)]
        public string WorkingDir { get; set; }

        [Option('t', "threshold", HelpText =
            "Threshold of final score out of 100 at which to fail (return -1 exit code)")]
        public int? Threshold { get; set; }

        private static ILogger<ScanVerb> Logger { get; } = Global.LogFactory.CreateLogger<ScanVerb>();

        public override async Task Execute()
        {
            WorkingDirectory.WorkingDirectoryBaseLocation = WorkingDir;
            var serviceMap = ServiceMapFile.GetServiceMap(Path.Combine(WorkingDir, "input.yaml"));
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

            var scorers = new List<Task<ScoredResult>>();
            foreach (var directory in Directory.GetDirectories(WorkingDir))
            {
                var workingDir = new ResourceWorkingDirectory(directory);
                if (workingDir.Configuration == null) continue;

                foreach (var pluginSummary in workingDir.Configuration.Plugins)
                {
                    var pluginDesc = PluginLibrary.PluginsAvailable[pluginSummary.Identifier];
                    //if (pluginDesc.PluginType != PluginTypes.Scorer)
                    //    continue;
                    scorers.Add(TaskManager.Instance.Enqueue(pluginSummary));
                }
            }

            scorers.ForEach(s => s.Start());
            await Task.WhenAll(scorers);
            var results = scorers.Select(t => t.Result).ToList();
            foreach (var result in results)
            {
                TreeDump.Dump(result, PluginLibrary.PluginsAvailable[result.Plugin.Identifier].PluginType
                    .GetTypeColoredText(result.Plugin.Identifier));
            }

            var outputFile = Path.Combine(WorkingDir,
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