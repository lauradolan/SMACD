using SMACD.Artifacts.Metadata;
using System;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace SMACD.Artifacts
{
    /// <summary>
    ///     Represents a port accessible via a specific protocol, and its service information
    /// </summary>
    public class ServicePortArtifact : Artifact
    {
        /// <summary>
        ///     An Action which can be registered by the Extension to return an HTML component to view artifact
        /// </summary>
        public override string ArtifactSummaryViewTypeName => "SMACD.Artifacts.Views.ServicePortArtifactView";

        /// <summary>
        ///     Service Port metadata
        /// </summary>
        public Versionable<ServicePortMetadata> Metadata { get; set; } = new Versionable<ServicePortMetadata>();

        /// <summary>
        ///     Hostname/IP of this Service
        /// </summary>
        public HostArtifact Host => (HostArtifact) Parent;

        /// <summary>
        ///     Port Protocol Type
        /// </summary>
        public ProtocolType Protocol
        {
            get
            {
                foreach (var identifier in Identifiers)
                {
                    var match = Regex.Match(identifier, "(?<protocol>[A-Za-z]*)\\/(?<port>[0-9]*)");
                    if (!match.Success) continue;
                    return Enum.Parse<ProtocolType>(match.Groups["protocol"].Value);
                }
                return ProtocolType.Unspecified;
            }
        }

        /// <summary>
        ///     Port number
        /// </summary>
        public int Port
        {
            get
            {
                foreach (var identifier in Identifiers)
                {
                    var match = Regex.Match(identifier, "(?<protocol>[A-Za-z]*)\\/(?<port>[0-9]*)");
                    if (!match.Success) continue;
                    return Int32.Parse(match.Groups["port"].Value);
                }
                return 0;
            }
        }

        /// <summary>
        ///     String representation of Service Port Artifact
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Metadata.Coalesced() == null
                ? $"{Protocol}/{Port}"
                : $"Service '{((ServicePortMetadata)Metadata).ServiceName}' on {Protocol}/{Port}";
        }
    }
}