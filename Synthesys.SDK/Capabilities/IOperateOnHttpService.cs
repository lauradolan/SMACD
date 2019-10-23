using SMACD.AppTree;

namespace Synthesys.SDK.Capabilities
{
    /// <summary>
    ///     IOperateOnHttpService indicates that the Extension acts upon a web server
    /// </summary>
    public interface IOperateOnHttpService
    {
        /// <summary>
        ///     Framework-populated reference to the HttpService Node
        /// </summary>
        HttpServiceNode HttpService { get; set; }
    }
}