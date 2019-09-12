using SMACD.Artifacts;

namespace Synthesys.SDK.Capabilities
{
    /// <summary>
    /// IOperateOnHost indicates that the Extension acts upon a single host
    /// </summary>
    public interface IOperateOnHost
    {
        /// <summary>
        /// Framework-populated reference to the Host Artifact
        /// </summary>
        HostArtifact Host { get; set; }
    }
}