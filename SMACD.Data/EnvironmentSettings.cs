using System.Collections.Generic;

namespace SMACD.Data
{
    /// <summary>
    ///     Configuration options for applications using this Service Map
    /// </summary>
    public class EnvironmentSettings
    {
        /// <summary>
        ///     Extensions which will be allowed to execute when running this Service Map
        /// </summary>
        public List<string> Whitelist { get; set; } = new List<string>();

        /// <summary>
        ///     Extensions which are not allowed to execute when running this Service Map
        /// </summary>
        public List<string> Blacklist { get; set; } = new List<string>();
    }
}