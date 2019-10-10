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
        ///     Hostname/IP of this Service
        /// </summary>
        public HostNode Host => (HostNode)Parent;

        /// <summary>
        ///     Details around a Service
        /// </summary>
        public Versionable<ServiceDetails> Detail { get; set; } = new Versionable<ServiceDetails>();

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