using SMACD.Artifacts;

namespace SMACD.SDK
{
    public interface IOperateOnHostPort
    {
        /// <summary>
        /// Service/Port being acted upon
        /// </summary>
        ServicePortArtifact Port { get; set; }
    }
}
