using System;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SMACD.Shared;
using SMACD.Shared.Plugins.Scorers;

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

        private static ILogger<ScanVerb> Logger { get; } = Workspace.LogFactory.CreateLogger<ScanVerb>();

        public override Task Execute()
        {
            Workspace.Instance.Load(WorkingDir);
            var summary = ScorerPluginManager.Instance.ScoreEntireMap().Result;

            var outputFile = Path.Combine(Workspace.Instance.WorkingDirectory,
                "summary_" + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + ".json");
            using (var sw = new StreamWriter(outputFile))
            {
                sw.WriteLine(JsonConvert.SerializeObject(summary));
            }

            Console.WriteLine("Averaged score: {0}", summary.ScoreAvg);
            Console.WriteLine("Summed score: {0}", summary.ScoreSum);

            Logger.LogInformation("Report serialized to {0}", outputFile);

            if (Threshold.HasValue)
            {
                Logger.LogDebug("Checking threshold");
                if (Threshold > summary.ScoreAvg)
                {
                    Logger.LogInformation("Failed threshold test! Expected: {0} / Actual: {1}", Threshold, summary.ScoreAvg);
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