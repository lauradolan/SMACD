using SMACD.Artifacts;

namespace SMACD.SDK
{
    public interface IOperateOnHost
    {
        /// <summary>
        /// Host artifact being acted upon
        /// </summary>
        HostArtifact Host { get; set; }
    }
}
