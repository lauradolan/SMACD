namespace SMACD.SDK.Capabilities
{
    public interface ICanQueueTasks
    {
        /// <summary>
        /// Task queue for running system
        /// </summary>
        ITaskToolbox Tasks { get; set; }
    }
}
