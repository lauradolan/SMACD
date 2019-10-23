using SMACD.AppTree;
using Synthesys.SDK;
using Synthesys.SDK.Triggers;
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
        ///     Enqueue an ActionExtension based on its Descriptor
        /// </summary>
        /// <param name="extensionIdentifier">Extension identifier</param>
        /// <param name="appTreeNode">Root app tree node for extension</param>
        /// <param name="options">Action options</param>
        /// <param name="serviceMapItemPtr">Pointer to element in Service Map which queued the Extension</param>
        /// <returns>Task which resolves to the Action-Specific Report</returns>
        Task<ExtensionReport> Enqueue(string extensionIdentifier, AppTreeNode appTreeNode, Dictionary<string, string> options, ProjectPointer serviceMapItemPtr = null);

        /// <summary>
        ///     Enqueue a ReactionExtension based on its Descriptor
        /// </summary>
        /// <param name="trigger">Trigger creating the Reaction</param>
        /// <param name="extensionIdentifier">ReactionExtension identifier</param>
        /// <param name="rootNode">Root of AppTree</param>
        /// <param name="options">Reaction options</param>
        /// <param name="serviceMapItemPtr">Pointer to element in Service Map which queued the Extension</param>
        /// <returns>Task which resolves to the Action-Specific Report</returns>
        Task<ExtensionReport> Enqueue(TriggerDescriptor trigger, string extensionIdentifier, RootNode rootNode, Dictionary<string, string> options, ProjectPointer serviceMapItemPtr = null);
    }
}