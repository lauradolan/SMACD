using System.Collections.Generic;

namespace Synthesys.Plugins.SQLMap
{
    public class SqlMapReport
    {
        /// <summary>
        ///     Points where a SQL injection can succeed
        /// </summary>
        public List<SqlMapInjectionVector> InjectionVectors { get; set; } = new List<SqlMapInjectionVector>();
    }
}