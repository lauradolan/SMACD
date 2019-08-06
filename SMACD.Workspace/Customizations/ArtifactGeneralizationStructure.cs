using SMACD.Workspace.Artifacts;
using SMACD.Workspace.Customizations.Correlations;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace SMACD.Workspace.Customizations
{
    public class Correlation
    {
        protected Artifact _artifact;
        public Correlation(Artifact artifact) => _artifact = artifact;
    }
    public class ArtifactCorrelation : Correlation
    {
        public HostArtifactCorrelation WithHost(string host) => 
            new HostArtifactCorrelation(_artifact["_HOSTS_"][host]);

        internal ArtifactCorrelation(Artifact artifact) : base(artifact) { }
    }

    public class HostArtifactCorrelation : Correlation
    {
        public VulnerabilityCollectionArtifactCorrelation Vulnerabilities => 
            new VulnerabilityCollectionArtifactCorrelation(_artifact["_VULNERABILITIES_"]);

        public PortCollectionArtifactCorrelation Ports => 
            new PortCollectionArtifactCorrelation(_artifact["_PORTS_"]);

        internal HostArtifactCorrelation(Artifact artifact) : base(artifact) { }
    }

    public class VulnerabilityCollectionArtifactCorrelation : Correlation
    {
        public Artifact AddVulnerability(Vulnerability vulnerability) =>
            _artifact.Save(vulnerability.ShortName, vulnerability);

        public IEnumerable<Vulnerability> Vulnerabilities =>
            _artifact.Children.Cast<ObjectArtifact>().Select(a => a.Get<Vulnerability>());

        internal VulnerabilityCollectionArtifactCorrelation(Artifact artifact) : base(artifact) { }
    }

    public class PortCollectionArtifactCorrelation : Correlation
    {
        public Artifact AddPort(ProtocolType protocol, int port) =>
            _artifact.Save($"{protocol}/{port}");

        internal PortCollectionArtifactCorrelation(Artifact artifact) : base(artifact) { }
    }
}
