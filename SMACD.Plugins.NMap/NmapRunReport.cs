using SMACD.Artifacts;
using SMACD.SDK;
using System;
using System.Collections.Generic;
using System.Text;

namespace SMACD.Plugins.Nmap
{
    public class NmapRunReport : ExtensionReport
    {
        public DateTime TimeOfExecution { get; set; }
        public List<NmapPort> Ports { get; set; } = new List<NmapPort>();
        public List<Vulnerability> Vulnerabilities { get; set; } = new List<Vulnerability>();

        public override string GetReportContent()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Open Ports:");
            foreach (NmapPort port in Ports)
            {
                sb.AppendLine($"{port.Protocol}/{port.Port}");
            }
            return sb.ToString();
        }
    }

    public class NmapPort
    {
        public string Protocol { get; set; }
        public int Port { get; set; }
        public string Service { get; set; }
        public int ServiceFingerprintConfidence { get; set; }
    }
}