using SMACD.Artifacts;

namespace SMACD.SDK
{
    public interface IOperateOnUrl
    {
        /// <summary>
        /// URL being acted upon
        /// </summary>
        UrlArtifact Url { get; set; }
    }
}
