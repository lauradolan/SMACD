using System.Collections.Generic;
using System.Linq;
using System.Text;
using Synthesys.SDK;

namespace Synthesys.Plugins.SQLMap
{
    public class SqlMapReport : ExtensionReport
    {
        /// <summary>
        ///     Points where a SQL injection can succeed
        /// </summary>
        public List<SqlMapInjectionVector> InjectionVectors { get; set; } = new List<SqlMapInjectionVector>();

        public override string GetReportContent()
        {
            if (!InjectionVectors.Any()) return "No injection found";

            var sb = new StringBuilder();
            foreach (var item in InjectionVectors)
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