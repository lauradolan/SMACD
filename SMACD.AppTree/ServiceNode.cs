using SMACD.AppTree.Details;
using System;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace SMACD.AppTree
{
    /// <summary>
    ///     Represents a single service, listening on a port of a Host
    /// </summary>
    public class ServiceNode : AppTreeNode, IAppTreeNode<ServiceDetails>
    {
        /// <summary>
        ///     Service types which are known by the system for investigation
        /// </summary>
        public enum KnownServiceNodeTypes
        {
            /// <summary>
            ///     Service fingerprint failed
            /// </summary>
            Unknown,

            /// <summary>
            ///     Web (HTTP/S) service
            /// </summary>
            Http
        }

        /// <summary>
        ///     A Razor component view which can be used to visualize the content of a given node
        /// </summary>
        public override string NodeViewName => "Compass.AppTree.ServiceNodeView";

        /// <summary>
        ///     Hostname/IP of this Service
        /// </summary>
        public HostNode Host => (HostNode)Parent;

        /// <summary>
        ///     Specific type of the service node for more information
        /// </summary>
        public KnownServiceNodeTypes ServiceNodeType
        {
            get
            {
                if (this is HttpServiceNode)
                {
                    return KnownServiceNodeTypes.Http;
                }

                return KnownServiceNodeTypes.Unknown;
            }
        }

        /// <summary>
        ///     Port Protocol Type
        /// </summary>
        public ProtocolType Protocol
        {
            get
            {
                foreach (string identifier in Identifiers)
                {
                    Match match = Regex.Match(identifier, "(?<protocol>[A-Za-z]*)\\/(?<port>[0-9]*)");
                    if (!match.Success)
                    {
                        continue;
                    }

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
                foreach (string identifier in Identifiers)
                {
                    Match match = Regex.Match(identifier, "(?<protocol>[A-Za-z]*)\\/(?<port>[0-9]*)");
                    if (!match.Success)
                    {
                        continue;
                    }

                    return int.Parse(match.Groups["port"].Value);
                }
                return 0;
            }
        }

        /// <summary>
        ///     Details around a Service
        /// </summary>
        public Versionable<ServiceDetails> Detail { get; set; } = new Versionable<ServiceDetails>();


        /// <summary>
        ///     String representation of Service Port Artifact
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Detail.Coalesced() == null
                ? $"{Protocol}/{Port}"
                : $"Service '{((Details.ServiceDetails)Detail).ServiceName}' on {Protocol}/{Port}";
        }
    }
}