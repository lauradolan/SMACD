using SMACD.Artifacts;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Synthesys.SDK;

namespace Synthesys.Plugins.SQLMap
{
    public class SqlMapReport : ExtensionReport
    {
        public List<SqlMapInjectionVector> InjectionVectors { get; set; } = new List<SqlMapInjectionVector>();

        public override string GetReportContent()
        {
            if (!InjectionVectors.Any())
            {
                return "No injection found";
            }

            StringBuilder sb = new StringBuilder();
            foreach (SqlMapInjectionVector item in InjectionVectors)
            {
                sb.AppendLine($"Injection Vector at Parameter '{item.Parameter}':");
                sb.AppendLine($"\tType: {item.Type}");
                sb.AppendLine($"\tVector: {item.Title}");
                sb.AppendLine($"\tPayload: {item.Payload}");
            }
            return sb.ToString();
        }
    }
}
