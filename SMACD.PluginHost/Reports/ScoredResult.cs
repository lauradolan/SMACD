using SMACD.PluginHost.Plugins;
using System;
using System.Collections.Generic;

namespace SMACD.PluginHost.Reports
{
    public class ScoredResult
    {
        public PluginSummary Plugin { get; set; }
        public PluginSummary Scorer { get; set; }
        public TimeSpan Runtime { get; set; }

        public Dictionary<string, object> Extra { get; set; }

        public double AdjustedScore => (RawScore / RawScoreMaximum) * 100;
        public double RawScore { get; set; }
        public double RawScoreMaximum { get; set; }

        public List<Vulnerability> Vulnerabilities { get; set; } = new List<Vulnerability>();

        /// <summary>
        /// List of Plugins that have been generated and queued by this Plugin (to interrogate some result)
        /// </summary>
        public List<ScoredResult> GeneratedInterrogativePlugins { get; set; } = new List<ScoredResult>();
    }
}
