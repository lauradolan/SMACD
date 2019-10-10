using SMACD.AppTree;
using Synthesys.SDK;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Synthesys.Tasks
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
        event EventHandler<RuntimeTaskDescriptor> TaskCompleted;

        /// <summary>
        ///     Fired when Task is faulted (errored)
        /// </summary>
        event EventHandler<RuntimeTaskDescriptor> TaskFaulted;

        /// <summary>
        ///     Fired when Task is started
        /// </summary>
        event EventHandler<RuntimeTaskDescriptor> TaskStarted;

        /// <summary>
        ///     Enqueue a new Task
        /// </summary>
        /// <param name="descriptor">Task Descriptor for new Task</param>
        /// <returns></returns>
        Task<List<ExtensionReport>> Enqueue(string actionIdentifier, AppTreeNode rootArtifact, Dictionary<string, string> options, ProjectPointer serviceMapItemPtr = null);
    }
}