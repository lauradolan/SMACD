using System.Collections.Generic;
using System.Text;
using SMACD.Artifacts;
using Synthesys.SDK;

namespace Synthesys.Plugins.Nikto
{
    public class NiktoReport : ExtensionReport
    {
        public string NiktoVersion { get; set; }
        public string ScanStart { get; set; }
        public string ScanEnd { get; set; }
        public string ServerBanner { get; set; }
        public string SiteName { get; set; }

        public List<Vulnerability> Vulnerabilities { get; set; } = new List<Vulnerability>();

        public override string GetReportContent()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Server Banner: {ServerBanner}");
            foreach (var vuln in Vulnerabilities)
                sb.AppendLine(
                    $"{vuln.Title} ({vuln.Occurrences} time(s)) Confidence/Risk Level: {vuln.Confidence}/{vuln.RiskLevel}");
            return sb.ToString();
        }
    }
}