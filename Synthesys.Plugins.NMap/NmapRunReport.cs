using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Synthesys.SDK;

namespace Synthesys.Plugins.Nmap
{
    public class NmapRunReport : ExtensionReport
    {
        /// <summary>
        ///     Open ports on the scanned host
        /// </summary>
        public List<NmapPort> Ports { get; set; } = new List<NmapPort>();

        public override string ReportViewName => typeof(NmapReportView).FullName;
        public override string ReportSummaryName => "Synthesys.Plugins.Nmap.NmapReportSummary";//typeof(NmapReportSummary).FullName;

        public override string GetReportContent()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Open Ports:");
            foreach (var port in Ports) sb.AppendLine($"{port.Protocol}/{port.Port}");
            return sb.ToString();
        }

        protected override ExtensionReport DeserializeFromString(string serializedData) =>
            JsonConvert.DeserializeObject<NmapRunReport>(serializedData, new JsonSerializerSettings() { TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple });
    }

    public class NmapPort
    {
        /// <summary>
        ///     Network protocol (TCP, UDP, etc.)
        /// </summary>
        public string Protocol { get; set; }

        /// <summary>
        ///     Port number
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        ///     Service fingerprint
        /// </summary>
        public string Service { get; set; }

        /// <summary>
        ///     Service fingerprint confidence
        /// </summary>
        public int ServiceFingerprintConfidence { get; set; }
    }
}