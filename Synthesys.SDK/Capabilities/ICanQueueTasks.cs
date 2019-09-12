namespace Synthesys.SDK.Capabilities
{
    /// <summary>
    /// ICanQueueTasks indicates that the Extension needs to interface with the Task Queue
    /// </summary>
    public interface ICanQueueTasks
    {
        /// <summary>
        /// Framework-populated reference to the Task Queue
        /// </summary>
        ITaskToolbox Tasks { get; set; }
    }
}