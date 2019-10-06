using System.Collections.Generic;

namespace SMACD.Data.Resources
{
    /// <summary>
    ///     Represents a Target resolved to its handler
    /// </summary>
    public class HttpTargetModel : TargetModel
    {
        /// <summary>
        ///     Field values to send when POSTing to this URL
        /// </summary>
        public Dictionary<string, string> Fields = new Dictionary<string, string>();

        /// <summary>
        ///     Headers to send when accessing this URL (regardless of Method)
        /// </summary>
        public Dictionary<string, string> Headers = new Dictionary<string, string>();

        /// <summary>
        ///     Method used to query the URL
        /// </summary>
        public string Method { get; set; } = "GET";

        /// <summary>
        ///     URL being accessed by Target
        /// </summary>
        public string Url { get; set; }
    }
}