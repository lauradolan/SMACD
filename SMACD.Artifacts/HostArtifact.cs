using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Sockets;

namespace SMACD.Artifacts
{
    /// <summary>
    /// Represents a single host (server) involved in some part of the application
    /// </summary>
    public class HostArtifact : Artifact
    {
        private string hostname;
        private string ipAddress;

        // <summary>
        /// An Action which can be registered by the Extension to return a text summary of the Artifact
        /// </summary>
        public override string ArtifactTextSummary { get; }

        /// <summary>
        /// An Action which can be registered by the Extension to return an HTML component to view artifact
        /// </summary>
        public override string ArtifactSummaryViewTypeName => "HostArtifactView";

        /// <summary>
        /// Represents a single host (server) involved in some part of the application
        /// </summary>
        public HostArtifact()
        {
            Aliases.CollectionChanged += (s, e) => NotifyChanged();
            ArtifactTextSummary = $"{Identifier} has the following aliases: {string.Join(", ", Aliases)}";
        }

        /// <summary>
        ///     Artifact Identifier
        /// </summary>
        public override string Identifier => Hostname;

        /// <summary>
        ///     Name of Host
        /// </summary>
        public string Hostname
        {
            get => hostname;
            set
            {
                if (!Aliases.Contains(value) && !string.IsNullOrEmpty(value)) Aliases.Add(value);

                hostname = value;
                NotifyChanged();
            }
        }

        /// <summary>
        ///     IP Address of Host
        /// </summary>
        public string IpAddress
        {
            get => ipAddress;
            set
            {
                if (!Aliases.Contains(value) && !string.IsNullOrEmpty(value)) Aliases.Add(value);

                ipAddress = value;
                NotifyChanged();
            }
        }

        public override string ArtifactSummaryViewTypeName => "SMACD.Artifacts.Views.HostArtifactView";

        /// <summary>
        ///     Aliases belonging to this Host
        /// </summary>
        public ObservableCollection<string> Aliases { get; } = new ObservableCollection<string>();

        /// <summary>
        ///     Get a TCP port/service by its port number
        /// </summary>
        /// <param name="port">TCP Port number</param>
        /// <returns></returns>
        public ServicePortArtifact this[int port]
        {
            get
            {
                var result = (ServicePortArtifact) Children.FirstOrDefault(p => ((ServicePortArtifact) p).Port == port);
                if (result == null)
                {
                    result = new ServicePortArtifact
                    {
                        Parent = this,
                        Port = port,
                        Protocol = ProtocolType.Tcp
                    };
                    result.BeginFiringEvents();
                    Children.Add(result);
                }

                return result;
            }
            set
            {
                var existing = (ServicePortArtifact) Children.FirstOrDefault(p =>
                    ((ServicePortArtifact) p).Port == port &&
                    ((ServicePortArtifact) p).Protocol == ProtocolType.Tcp);
                if (existing != null) Children.Remove(existing);

                value.Parent = this;
                if (value.Port == 0) value.Port = port;

                if (value.Protocol == ProtocolType.Unspecified) value.Protocol = ProtocolType.Tcp;

                value.BeginFiringEvents();
                Children.Add(value);
            }
        }

        /// <summary>
        ///     Get a port/service by its port number and type
        /// </summary>
        /// <param name="protocolAndport">Protocol and port, i.e. Tcp/80</param>
        /// <returns></returns>
        public ServicePortArtifact this[string protocolAndport]
        {
            get
            {
                var protocol = Enum.Parse<ProtocolType>(protocolAndport.Split('/')[0]);
                var port = int.Parse(protocolAndport.Split('/')[1]);

                var result = (ServicePortArtifact) Children.FirstOrDefault(p =>
                    ((ServicePortArtifact) p).Port == port &&
                    ((ServicePortArtifact) p).Protocol == protocol);
                if (result == null)
                {
                    result = new ServicePortArtifact {Port = port, Protocol = protocol, Parent = this};
                    Children.Add(result);
                }

                return result;
            }
            set
            {
                var existing = (ServicePortArtifact) Children.FirstOrDefault(p =>
                    ((ServicePortArtifact) p).Port == int.Parse(protocolAndport.Split('/')[1]) &&
                    ((ServicePortArtifact) p).Protocol == Enum.Parse<ProtocolType>(protocolAndport.Split('/')[0]));
                if (existing != null) Children.Remove(existing);

                value.Parent = this;
                if (value.Port == 0) value.Port = int.Parse(protocolAndport.Split('/')[1]);

                if (value.Protocol == ProtocolType.Unspecified)
                    value.Protocol = Enum.Parse<ProtocolType>(protocolAndport.Split('/')[0]);

                value.BeginFiringEvents();
                Children.Add(value);
            }
        }

        public override string ToString()
        {
            return $"Host {Hostname} ({IpAddress})";
        }
    }
}