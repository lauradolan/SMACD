using CommandLine;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SMACD.Shared;
using SMACD.Shared.Resources;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SMACD.CLITool.Verbs
{
    [Verb("scan", HelpText = "Run scan using plugins specified in a Service Map")]
    public class ScanVerb : VerbBase
    {
        [Option('s', "servicemap", HelpText = "Location of the Service Map", Required = true)]
        public string ServiceMap { get; set; }

        [Option('c', "workingcontainer", HelpText = "Root directory of all plugin working directories")]
        public string WorkingContainer { get; set; }

        [Option('t', "threshold", HelpText = "Threshold of final score out of 100 at which to fail (return -1 exit code)")]
        public int? Threshold { get; set; }

        private static ILogger<ScanVerb> Logger { get; set; } = Shared.Extensions.LogFactory.CreateLogger<ScanVerb>();

        public override Task Execute()
        {
            // Hotpatch workspace storage
            if (WorkingContainer == null) WorkingContainer = Path.Combine(Path.GetTempPath(), "SMACD");
            Workspace.WORKSPACE_STORAGE = WorkingContainer;

            int workerThreads, completionPortThreads;
            ThreadPool.GetMinThreads(out workerThreads, out completionPortThreads);
            Logger.LogDebug("ThreadPool configuration: {0} workers, {1} completion port threads", workerThreads, completionPortThreads);

            ThreadPool.GetAvailableThreads(out workerThreads, out completionPortThreads);
            Logger.LogDebug("ThreadPool availability: {0} workers, {1} completion port threads", workerThreads, completionPortThreads);

            Logger.LogInformation($"Running scan with service map {ServiceMap}");
            Workspace.Instance.Create(ServiceMap);
            var result = Workspace.Instance.ScanEntireMap().Result;

            if (!Silent)
                TerminalEffects.DrawSingleLineBanner("= Orphan URLs =");
            Logger.LogInformation("Found {0} URLs not found in Resource Map", result.DiscoveredResources.Count(r => r.SystemCreated));
            foreach (var resource in result.DiscoveredResources)
            {
                // todo: move formatting to the resource (via log)
                if (resource is HttpResource)
                    Logger.LogInformation("URL: {0} {1}", ((HttpResource)resource).Method, ((HttpResource)resource).Url);
            }

            if (!Silent)
                TerminalEffects.DrawSingleLineBanner("= Results =");

            var outputFile = Path.Combine(Workspace.Instance.WorkingDirectory, "summary_" + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + ".json");
            using (var sw = new StreamWriter(outputFile))
                sw.WriteLine(JsonConvert.SerializeObject(result));

            Console.WriteLine(ObjectDumper.Dump(result));

            Console.WriteLine("Average score: {0}", result.ScoreAvg);
            Console.WriteLine("Summed score: {0}", result.ScoreSum);

            Logger.LogInformation("Report serialized to {0}", outputFile);

            if (Threshold.HasValue)
            {
                Logger.LogDebug("Checking threshold");
                if (Threshold > result.ScoreAvg)
                {
                    Logger.LogDebug("Failed! Expected: {0} / Actual: {1}", Threshold, result.ScoreAvg);
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