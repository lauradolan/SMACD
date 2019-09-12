using SMACD.Artifacts;

namespace Synthesys.SDK.Capabilities
{
    /// <summary>
    /// IOperateOnUrl indicates that the Extension acts upon a URL
    /// </summary>
    public interface IOperateOnUrl
    {
        /// <summary>
        /// Framework-populated reference to the URL artifact
        /// </summary>
        UrlArtifact Url { get; set; }
    }
}