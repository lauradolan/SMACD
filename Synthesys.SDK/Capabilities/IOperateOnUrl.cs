using SMACD.AppTree;

namespace Synthesys.SDK.Capabilities
{
    /// <summary>
    ///     IOperateOnUrl indicates that the Extension acts upon a URL
    /// </summary>
    public interface IOperateOnUrl
    {
        /// <summary>
        ///     Framework-populated reference to the URL node
        /// </summary>
        UrlNode Url { get; set; }
    }
}