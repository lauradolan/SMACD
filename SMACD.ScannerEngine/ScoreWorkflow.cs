using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SMACD.Data;
using SMACD.ScannerEngine.Attributes;
using SMACD.ScannerEngine.Factories;
using SMACD.ScannerEngine.Plugins;

namespace SMACD.ScannerEngine
{
    public class ScoreWorkflow
    {
        public ScoreWorkflow(string baseWorkingDirectory)
        {
            WorkingDirectory = baseWorkingDirectory;
        }

        public string WorkingDirectory { get; set; }

        private ILogger Logger { get; } = Global.LogFactory.CreateLogger("ScoreWorkflow");

        public async Task<VulnerabilitySummary> Execute()
        {
            if (!File.Exists(Path.Combine(WorkingDirectory, "input.yaml")))
            {
                Logger.LogCritical("Service map mirror not found");
                throw new Exception("Service map mirror not found");
            }

            var serviceMap = Global.GetServiceMap(Path.Combine(WorkingDirectory, "input.yaml"));
            var scorers = new List<ScorerPlugin>();
            foreach (var feature in serviceMap.Features)
            foreach (var useCase in feature.UseCases)
            foreach (var abuseCase in useCase.AbuseCases)
            foreach (var pluginPointer in abuseCase.PluginPointers)
                scorers.Add(GetScorer(pluginPointer));
            scorers.RemoveAll(s => s == null);

            var summary = new VulnerabilitySummary();
            var tasks = scorers.Select(s => s.Score(summary));
            Task.WhenAll(tasks).Wait();

            // Run generations of multi-execution tasks
            var changesOccurredThisGeneration = true;
            var sw = new Stopwatch();
            sw.Start();
            var generations = 0;
            while (changesOccurredThisGeneration)
            {
                generations++;
                foreach (var scorer in scorers)
                    changesOccurredThisGeneration = scorer.Converge(summary).Result;
                Logger.LogDebug("Completed summary generation {0} ... Converged? {1}", generations,
                    !changesOccurredThisGeneration);
            }

            sw.Stop();
            Logger.LogInformation("Completed summary report in {0} over {1} generations", sw.Elapsed, generations);

            return summary;
        }

        private ScorerPlugin GetScorer(PluginPointerModel pointer)
        {
            var attackTool = AttackToolPluginFactory.Instance.Emit(pointer.Plugin, new Dictionary<string, string>());
            var toolMetadata = PluginMetadataAttribute.Get(attackTool.GetType());

            ScorerPlugin scorer;
            if (!string.IsNullOrEmpty(pointer.Scorer))
            {
                scorer = ScorerPluginFactory.Instance.Emit(pointer.Scorer);
            }
            else if (!string.IsNullOrEmpty(toolMetadata.DefaultScorer))
            {
                scorer = ScorerPluginFactory.Instance.Emit(toolMetadata.DefaultScorer);
            }
            else
            {
                Logger.LogCritical("Plugin instance of {0} does not specify a Scorer and no default Scorer is provided",
                    toolMetadata.Identifier);
                throw new Exception(
                    $"Plugin instance of {toolMetadata.Identifier} does not specify a Scorer and no default Scorer is provided");
            }

            scorer.WorkingDirectory = WorkingDirectory;
            scorer.Pointer = pointer;

            return scorer;
        }
    }
}