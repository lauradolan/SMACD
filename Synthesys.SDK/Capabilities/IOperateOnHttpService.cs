using SMACD.Artifacts;

namespace Synthesys.SDK.Capabilities
{
    /// <summary>
    ///     IOperateOnHttpService indicates that the Extension acts upon a web server
    /// </summary>
    public interface IOperateOnHttpService
    {
        /// <summary>
        ///     Framework-populated reference to the HttpServicePort Artifact
        /// </summary>
        HttpServicePortArtifact HttpService { get; set; }
    }
}