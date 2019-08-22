using SMACD.Artifacts;

namespace SMACD.SDK.Capabilities
{
    public interface IOperateOnHostPort
    {
        /// <summary>
        /// Service/Port being acted upon
        /// </summary>
        ServicePortArtifact Port { get; set; }
    }
}
