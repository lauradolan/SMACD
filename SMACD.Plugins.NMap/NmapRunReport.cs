using SMACD.Workspace.Actions;
using SMACD.Workspace.Customizations.Correlations;
using System;
using System.Collections.Generic;

namespace SMACD.Plugins.Nmap
{
    public class NmapRunReport : ActionSpecificReport
    {
        public DateTime TimeOfExecution { get; set; }
        public List<NmapPort> Ports { get; set; } = new List<NmapPort>();
        public List<Vulnerability> Vulnerabilities { get; set; } = new List<Vulnerability>();
    }

    public class NmapPort
    {
        public string Protocol { get; set; }
        public int Port { get; set; }
        public string Service { get; set; }
        public int ServiceFingerprintConfidence { get; set; }
    }
}