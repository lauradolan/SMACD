using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SMACD.ScannerEngine;
using SMACD.ScannerEngine.Extensions;

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

            var scanWorkflow = new ScanWorkflow(WorkingDirectory, ServiceMap);
            scanWorkflow.Execute().Wait();

            var scoreWorkflow = new ScoreWorkflow(WorkingDirectory);
            var result = scoreWorkflow.Execute().Result;

            Logger.LogInformation("Found {0} URLs not found in Resource Map",
                result.DiscoveredResources.Count(r => r.SystemCreated));
            foreach (var resource in result.DiscoveredResources)
                Logger.LogInformation(resource.GetDescription());

            var outputFile = Path.Combine(WorkingDirectory,
                "summary_" + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + ".json");
            using (var sw = new StreamWriter(outputFile))
            {
                sw.WriteLine(JsonConvert.SerializeObject(result));
            }

            Console.WriteLine("Average score: {0}", result.ScoreAvg);
            Console.WriteLine("Summed score: {0}", result.ScoreSum);

            Console.WriteLine("Report serialized to {0}", outputFile);

            if (Threshold.HasValue)
            {
                Logger.LogDebug("Checking threshold");
                if (Threshold > result.ScoreAvg)
                {
                    Logger.LogInformation("Failed threshold test! Expected: {0} / Actual: {1}", Threshold,
                        result.ScoreAvg);
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