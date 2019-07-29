using System;
using System.Collections.Generic;
using SMACD.PluginHost.Plugins;

namespace SMACD.PluginHost.Reports
{
    public class ScoredResult
    {
        public PluginSummary Plugin { get; set; }
        public PluginSummary Scorer { get; set; }
        public TimeSpan Runtime { get; set; }

        public Dictionary<string, object> Extra { get; set; }

        public double AdjustedScore => RawScore / RawScoreMaximum * 100;
        public double RawScore { get; set; }
        public double RawScoreMaximum { get; set; }

        public IList<Vulnerability> Vulnerabilities { get; set; } = new List<Vulnerability>();
    }
}