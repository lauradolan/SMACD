using System.Collections.Generic;

namespace Synthesys.Plugins.Nmap
{
    public class NmapRunReport
    {
        /// <summary>
        ///     Open ports on the scanned host
        /// </summary>
        public List<NmapPort> Ports { get; set; } = new List<NmapPort>();

        public List<string> OperatingSystemFingerprintCandidates { get; set; } = new List<string>();
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

        /// <summary>
        ///     Product name
        /// </summary>
        public string ProductName { get; set; }

        /// <summary>
        ///     Product version
        /// </summary>
        public string ProductVersion { get; set; }

        /// <summary>
        ///     Any additional info NMap understands about the service
        /// </summary>
        public string ExtraInfo { get; set; }
    }
}