using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Synthesys.SDK
{
    public interface ITaskToolbox
    {
        /// <summary>
        ///     If the Task queue is running
        /// </summary>
        bool IsRunning { get; }

        /// <summary>
        ///     Number of queued and running Tasks
        /// </summary>
        int Count { get; }

        /// <summary>
        ///     Fired when Task is completed
        /// </summary>
        event EventHandler<ResultProvidingTaskDescriptor> TaskCompleted;

        /// <summary>
        ///     Fired when Task is faulted (errored)
        /// </summary>
        event EventHandler<ResultProvidingTaskDescriptor> TaskFaulted;

        /// <summary>
        ///     Fired when Task is started
        /// </summary>
        event EventHandler<ResultProvidingTaskDescriptor> TaskStarted;

        /// <summary>
        ///     Enqueue a new Task
        /// </summary>
        /// <param name="descriptor">Task Descriptor for new Task</param>
        /// <returns></returns>
        Task<List<ExtensionReport>> Enqueue(TaskDescriptor descriptor);
    }
}