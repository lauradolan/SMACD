using SMACD.Workspace.Artifacts;
using SMACD.Workspace.Customizations.Correlations;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace SMACD.Workspace.Customizations
{
    public class ArtifactCorrelation : Artifact
    {
        public HostArtifactCorrelation WithHost(string host) => 
            (HostArtifactCorrelation)this["_HOSTS_"][host];
    }

    public class HostArtifactCorrelation : Artifact
    {
        public VulnerabilityCollectionArtifactCorrelation Vulnerabilities => 
            (VulnerabilityCollectionArtifactCorrelation) this["_VULNERABILITIES_"];

        public PortCollectionArtifactCorrelation Ports => 
            (PortCollectionArtifactCorrelation) this["_PORTS_"];
    }

    public class VulnerabilityCollectionArtifactCorrelation : Artifact
    {
        public Artifact AddVulnerability(Vulnerability vulnerability) =>
            this.Save(vulnerability.ShortName, vulnerability);

        public IEnumerable<Vulnerability> Vulnerabilities =>
            this.Children.Cast<ObjectArtifact>().Select(a => a.Get<Vulnerability>());
    }

    public class PortCollectionArtifactCorrelation : Artifact
    {
        public Artifact AddPort(ProtocolType protocol, int port) =>
            this.Save($"{protocol}/{port}");
    }
}
