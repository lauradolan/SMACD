using Newtonsoft.Json;
using SMACD.SDK;
using System.Collections.Generic;

namespace SMACD.Plugins.OwaspZap
{
    public class ZapJsonReport : ExtensionReport
    {
        public IEnumerable<ZapJsonSite> Site { get; set; }

        public override string GetReportContent()
        {
            return "";
        }
    }

    public class ZapJsonSite
    {
        [JsonProperty("@name")] public string Name { get; set; }

        [JsonProperty("@host")] public string Host { get; set; }

        [JsonProperty("@port")] public string Port { get; set; }

        [JsonProperty("@ssl")] public string IsSSL { get; set; }

        public IEnumerable<ZapJsonAlertWithInstances> Alerts { get; set; }
    }

    public class ZapJsonAlertWithInstances : ZapJsonAlert
    {
        public IEnumerable<ZapJsonAlertInstance> Instances { get; set; }
    }

    public class ZapJsonAlert
    {
        public string PluginId { get; set; }
        public string Alert { get; set; }
        public string Name { get; set; }
        public int RiskCode { get; set; }
        public int Confidence { get; set; }
        public string RiskDesc { get; set; }
        public string Desc { get; set; }

        public IEnumerable<ZapJsonAlertInstance> Instances { get; set; }

        public string Count { get; set; }
        public string Solution { get; set; }
        public string OtherInfo { get; set; }
        public string Reference { get; set; }
        public string CWEId { get; set; }
        public string WASCId { get; set; }
        public string SourceId { get; set; }
    }

    public class ZapJsonAlertInstance
    {
        public string Uri { get; set; }
        public string Method { get; set; }
        public string Evidence { get; set; }
        public string Param { get; set; }
    }
}