using System.Collections.Generic;

namespace SMACD.Workspace.Targets
{
    /// <summary>
    /// Describes a web (HTTP) target
    /// </summary>
    public class HttpTarget : TargetDescriptor
    {
        /// <summary>
        /// URL of target
        /// </summary>
        public string Url { get; set; }
        
        /// <summary>
        /// Method to use against target
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Headers to send
        /// </summary>
        public Dictionary<string, string> Headers { get; set; }

        /// <summary>
        /// Fields to send
        /// </summary>
        public Dictionary<string, string> Fields { get; set; }
    }
}
