namespace SMACD.SDK
{
    public interface ICanQueueTasks
    {
        /// <summary>
        /// Task queue for running system
        /// </summary>
        ITaskToolbox Tasks { get; set; }
    }
}
