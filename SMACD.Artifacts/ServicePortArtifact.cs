using System.Net.Sockets;

namespace SMACD.Artifacts
{
    public class ServicePortArtifact : Artifact
    {
        public override string Identifier => $"{Protocol}/{Port}";
        public HostArtifact Host => (HostArtifact)Parent;

        private int port;
        private ProtocolType protocol;
        private string serviceName;
        private string serviceBanner;

        public ProtocolType Protocol
        {
            get => protocol;
            set
            {
                protocol = value;
                NotifyChanged();
            }
        }

        public int Port
        {
            get => port;
            set
            {
                port = value;
                NotifyChanged();
            }
        }

        public string ServiceName
        {
            get => serviceName;
            set
            {
                serviceName = value;
                NotifyChanged();
            }
        }

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
