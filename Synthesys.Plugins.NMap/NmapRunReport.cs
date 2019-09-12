using System.Collections.Generic;
using System.Text;
using Synthesys.SDK;

namespace Synthesys.Plugins.Nmap
{
    public class NmapRunReport : ExtensionReport
    {
        /// <summary>
        /// Open ports on the scanned host
        /// </summary>
        public List<NmapPort> Ports { get; set; } = new List<NmapPort>();
        
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
        /// <summary>
        /// Network protocol (TCP, UDP, etc.)
        /// </summary>
        public string Protocol { get; set; }

        /// <summary>
        /// Port number
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Service fingerprint
        /// </summary>
        public string Service { get; set; }

        /// <summary>
        /// Service fingerprint confidence
        /// </summary>
        public int ServiceFingerprintConfidence { get; set; }
    }
}