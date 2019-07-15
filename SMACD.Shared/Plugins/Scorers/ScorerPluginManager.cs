using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SMACD.Shared.Data;
using SMACD.Shared.Plugins.AttackTools;

namespace SMACD.Shared.Plugins.Scorers
{
    /// <summary>
    ///     Handles scanning and mapping of plugins
    /// </summary>
    public class ScorerPluginManager : LibraryManager<Scorer>
    {
        private static readonly Lazy<ScorerPluginManager> _instance =
            new Lazy<ScorerPluginManager>(() => new ScorerPluginManager());

        private ScorerPluginManager() : base("SMACD.Plugins.*.dll", "ScorerPluginManager")
        {
        }

        public static ScorerPluginManager Instance => _instance.Value;

        public Scorer GetInstance(PluginPointerModel pointer) => GetInstance(pointer.Plugin, pointer.Scorer);

        /// <summary>
        ///     Reprocess all items on the loaded map. Reprocessing involves reparsing the scanner-generated artifacts and
        ///     regenerating the summary report
        /// </summary>
        /// <returns></returns>
        public async Task<VulnerabilitySummary> ScoreEntireMap()
        {
            Logger.LogInformation("Beginning to score data from working directory {0}",
                Workspace.Instance.WorkingDirectory);
            var scorers = new List<Scorer>();
            foreach (var feature in Workspace.Instance.Features)
            foreach (var useCase in feature.UseCases)
            foreach (var abuseCase in useCase.AbuseCases)
            foreach (var pluginPointer in abuseCase.PluginPointers)
                scorers.Add(GetInstance(pluginPointer));
            scorers.RemoveAll(s => s == null);

            var summary = new VulnerabilitySummary();

            // Run single-execution tasks for each of the result objects
            Logger.LogInformation("Executing single-run tasks for {0} result objects", scorers.Count);
            await Task.WhenAll(scorers.Select(r => r.GenerateScoreWrapped(summary)));
            Logger.LogInformation("Completed single-run execution against {0} result objects", scorers.Count);

            // Run generations of multi-execution tasks
            var changesOccurredThisGeneration = true;
            var sw = new Stopwatch();
            sw.Start();
            var generations = 0;
            while (changesOccurredThisGeneration)
            {
                generations++;
                foreach (var g in scorers)
                    changesOccurredThisGeneration = await g.ConvergeSummaryWrapped(summary);
                Logger.LogDebug("Completed summary generation {0} ... Converged? {1}", generations,
                    !changesOccurredThisGeneration);
            }

            sw.Stop();
            Logger.LogInformation("Completed summary report in {0} over {1} generations", sw.Elapsed, generations);
            return summary;
        }
    }
}