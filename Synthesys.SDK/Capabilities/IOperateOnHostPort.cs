using SMACD.AppTree;

namespace Synthesys.SDK.Capabilities
{
    /// <summary>
    ///     ICanQueueTasks indicates that the Extension acts upon an unknown service (by port)
    /// </summary>
    public interface IOperateOnHostPort
    {
        /// <summary>
        ///     Framework-populated reference to the Service Node
        /// </summary>
        ServiceNode Service { get; set; }
    }
}