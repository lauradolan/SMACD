using CommandLine;
using Crayon;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SMACD.Data;
using SMACD.PluginHost;
using SMACD.PluginHost.Extensions;
using SMACD.PluginHost.Plugins;
using SMACD.PluginHost.Reports;
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

            var scorers = new List<Task<ScoredResult>>();
            foreach (var directory in Directory.GetDirectories(WorkingDir))
            {
                var workingDir = new ResourceWorkingDirectory(directory);
                if (workingDir.Configuration == null) continue;

                foreach (var pluginSummary in workingDir.PluginChain)
                {
                    var pluginDesc = PluginLibrary.PluginsAvailable[pluginSummary.Identifier];
                    if (pluginDesc.PluginType != PluginTypes.Scorer)
                        continue;
                    scorers.Add(TaskManager.Instance.Enqueue(pluginSummary));
                }
            }

            scorers.ForEach(s => s.Start());
            await Task.WhenAll(scorers);
            var results = scorers.Select(t => t.Result).ToList();
            foreach (var result in results)
            {
                Console.Write(Output.BrightWhite("Plugin: "));
                PluginLibrary.PluginsAvailable[result.Plugin.Identifier].PluginType.WriteTypeColoredText(result.Plugin.Identifier + Environment.NewLine);

                ObjectTreeRenderer.Print(result);
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
    }
}