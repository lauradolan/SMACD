using SMACD.AppTree;

namespace Synthesys.SDK.Capabilities
{
    /// <summary>
    ///     IOperateOnHost indicates that the Extension acts upon a single host
    /// </summary>
    public interface IOperateOnHost
    {
        /// <summary>
        ///     Framework-populated reference to the Host Node
        /// </summary>
        HostNode Host { get; set; }
    }
}