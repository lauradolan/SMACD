using System.Net.Sockets;

namespace SMACD.Artifacts
{
    public class ServicePortArtifact : Artifact
    {
        /// <summary>
        /// Artifact Identifier
        /// </summary>
        public override string Identifier => $"{Protocol}/{Port}";

        /// <summary>
        /// Hostname/IP of this Service
        /// </summary>
        public HostArtifact Host => (HostArtifact)Parent;

        private int port;
        private ProtocolType protocol;
        private string serviceName;
        private string serviceBanner;

        /// <summary>
        /// Port Protocol Type
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
        /// Port number
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
        /// Service name
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
        /// Service banner
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
            return string.IsNullOrEmpty(ServiceName) ?
$"{Protocol}/{Port}" :
$"Service '{ServiceName}' on {Protocol}/{Port}";
        }
    }
}
