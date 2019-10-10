using System;
using System.Linq;
using System.Net.Sockets;
using SMACD.AppTree.Details;

namespace SMACD.AppTree
{
    /// <summary>
    ///     Represents a single host (server) involved in some part of the application
    /// </summary>
    public class HostNode : AppTreeNode, IAppTreeNode<Details.HostDetails>
    {
        /// <summary>
        ///     A Razor component view which can be used to visualize the content of a given node
        /// </summary>
        public override string NodeViewName => "SMACD.AppTree.Views.HostNodeView";

        /// <summary>
        ///     Name of Host
        /// </summary>
        public string Hostname
        {
            get => Identifiers.Where(i => !Guid.TryParse(i, out Guid dummy)).FirstOrDefault(a => Uri.CheckHostName(a) == UriHostNameType.Dns);
            set
            {
                if (!Identifiers.Contains(value))
                {
                    Identifiers.Add(value);
                }
            }
        }

        /// <summary>
        ///     IP Address of Host
        /// </summary>
        public string IpAddress
        {
            get => Identifiers.Where(i => !Guid.TryParse(i, out Guid dummy)).FirstOrDefault(a => Uri.CheckHostName(a) == UriHostNameType.IPv4);
            set
            {
                if (!Identifiers.Contains(value))
                {
                    Identifiers.Add(value);
                }
            }
        }

        /// <summary>
        ///     Details around a Host
        /// </summary>
        public Versionable<HostDetails> Detail { get; set; } = new Versionable<HostDetails>();

        /// <summary>
        ///     Get a TCP port/service by its port number
        /// </summary>
        /// <param name="port">TCP Port number</param>
        /// <returns></returns>
        public ServiceNode this[int port]
        {
            get => this[$"{ProtocolType.Tcp}/{port}"];
            set => this[$"{ProtocolType.Tcp}/{port}"] = value;
        }

        /// <summary>
        ///     Get a port/service by its port number and type
        /// </summary>
        /// <param name="protocolAndPort">Protocol and port, i.e. Tcp/80</param>
        /// <returns></returns>
        public ServiceNode this[string protocolAndPort]
        {
            get
            {
                ProtocolType protocol = Enum.Parse<ProtocolType>(protocolAndPort.Split('/')[0]);
                int port = int.Parse(protocolAndPort.Split('/')[1]);

                ServiceNode result = ChildrenAre<ServiceNode>().FirstOrDefault(n => n.Port == port && n.Protocol == protocol);
                if (result == null)
                {
                    var newEntry = new ServiceNode() { Parent = this };
                    newEntry.Identifiers.Add($"{protocol}/{port}");
                    Children.Add(newEntry);
                }

                return result;
            }
            set
            {
                ProtocolType protocol = Enum.Parse<ProtocolType>(protocolAndPort.Split('/')[0]);
                int port = int.Parse(protocolAndPort.Split('/')[1]);

                var existing = Children.FirstOrDefault(c => c.Identifiers.Contains($"{protocol}/{port}"));
                if (existing != null)
                    Children.Remove(existing);

                if (value.Identifiers.Contains($"{protocol}/{port}"))
                    value.Identifiers.Add($"{protocol}/{port}");
                Children.Add(value);
            }
        }

        /// <summary>
        ///     String representation of host
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Host {Hostname} ({IpAddress})";
        }
    }
}