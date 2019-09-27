using System.Net.Sockets;

namespace SMACD.Artifacts
{
    public class ServicePortArtifact : Artifact
    {
        private int port;
        private ProtocolType protocol;
        private string serviceBanner;
        private string serviceName;

        // <summary>
        /// An Action which can be registered by the Extension to return a text summary of the Artifact
        /// </summary>
        public override string ArtifactTextSummary { get; }

        /// <summary>
        /// An Action which can be registered by the Extension to return an HTML component to view artifact
        /// </summary>
        public override string ArtifactSummaryViewTypeName => "SMACD.Artifacts.Views.ServicePortArtifactView";

        /// <summary>
        ///     Artifact Identifier
        /// </summary>
        public override string Identifier => $"{Protocol}/{Port}";

        /// <summary>
        ///     Hostname/IP of this Service
        /// </summary>
        public HostArtifact Host => (HostArtifact) Parent;

        /// <summary>
        ///     Port Protocol Type
        /// </summary>
        public ProtocolType Protocol
        {
            get => protocol;
            set
            {
                protocol = value;
                NotifyChanged();
            }
        }

        /// <summary>
        ///     Port number
        /// </summary>
        public int Port
        {
            get => port;
            set
            {
                port = value;
                NotifyChanged();
            }
        }

        /// <summary>
        ///     Service name
        /// </summary>
        public string ServiceName
        {
            get => serviceName;
            set
            {
                serviceName = value;
                NotifyChanged();
            }
        }

        /// <summary>
        ///     Service banner
        /// </summary>
        public string ServiceBanner
        {
            get => serviceBanner;
            set
            {
                serviceBanner = value;
                NotifyChanged();
            }
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(ServiceName)
                ? $"{Protocol}/{Port}"
                : $"Service '{ServiceName}' on {Protocol}/{Port}";
        }
    }
}