using SMACD.Artifacts;

namespace SMACD.SDK.Capabilities
{
    public interface IOperateOnHttpService
    {
        /// <summary>
        /// HTTP Service being acted upon
        /// </summary>
        HttpServicePortArtifact HttpService { get; set; }
    }
}
