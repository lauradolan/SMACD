using SMACD.Artifacts.Metadata;
using System;
using System.Linq;
using System.Net.Sockets;

namespace SMACD.Artifacts
{
    /// <summary>
    /// Represents a single host (server) involved in some part of the application
    /// </summary>
    public class HostArtifact : Artifact
    {
        /// <summary>
        ///     An Action which can be registered by the Extension to return an HTML component to view artifact
        /// </summary>
        public override string ArtifactSummaryViewTypeName => "SMACD.Artifacts.Views.HostArtifactView";

        /// <summary>
        ///     Host metadata
        /// </summary>
        public Versionable<UrlMetadata> Metadata { get; set; } = new Versionable<UrlMetadata>();

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
        ///     Get a TCP port/service by its port number
        /// </summary>
        /// <param name="port">TCP Port number</param>
        /// <returns></returns>
        public ServicePortArtifact this[int port]
        {
            get
            {
                ServicePortArtifact result = this[$"{ProtocolType.Tcp.ToString()}/{port}"];
                if (result == null)
                {
                    result = new ServicePortArtifact { Parent = this };
                    result.Identifiers.Add($"{ProtocolType.Tcp.ToString()}/{port}");
                    result.BeginFiringEvents();
                    Children.Add(result);
                }

                return result;
            }
            set
            {
                ServicePortArtifact existing = this[$"{value.Protocol}/{port}"];
                if (existing != null)
                {
                    Children.Remove(existing);
                }

                value.BeginFiringEvents();
                Children.Add(value);
            }
        }

        /// <summary>
        ///     Get a port/service by its port number and type
        /// </summary>
        /// <param name="protocolAndPort">Protocol and port, i.e. Tcp/80</param>
        /// <returns></returns>
        public ServicePortArtifact this[string protocolAndPort]
        {
            get
            {
                ProtocolType protocol = Enum.Parse<ProtocolType>(protocolAndPort.Split('/')[0]);
                int port = int.Parse(protocolAndPort.Split('/')[1]);

                ServicePortArtifact result = (ServicePortArtifact)Children.FirstOrDefault(p =>
                   ((ServicePortArtifact)p).Port == port &&
                   ((ServicePortArtifact)p).Protocol == protocol);
                if (result == null)
                {
                    result = new ServicePortArtifact { Parent = this };
                    result.Identifiers.Add($"{protocol}/{port}");
                    Children.Add(result);
                }

                return result;
            }
            set
            {
                ServicePortArtifact existing = this[protocolAndPort];
                if (existing != null)
                {
                    Children.Remove(existing);
                }

                value.BeginFiringEvents();
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