using Microsoft.Extensions.Logging;
using SMACD.Workspace.Artifacts;
using SMACD.Workspace.Libraries;
using SMACD.Workspace.Triggers;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SMACD.Workspace.Targets
{
    /// <summary>
    /// Manages Targets that are acted upon by Actions in the system
    /// </summary>
    public class TriggerToolbox : WorkspaceToolbox
    {
        internal TriggerToolbox(Workspace workspace) : base(workspace)
        {
            // Binding to these events means whenever one is fired, any Actions currently loaded
            //   that have TriggeredBy attributes matching the event will be added to the Task queue

            Artifact.ArtifactCreated += (s, e) => CurrentWorkspace.Tasks.EnqueueTasksTriggeredBy(e);
            Artifact.ArtifactChanged += (s, e) => CurrentWorkspace.Tasks.EnqueueTasksTriggeredBy(e);

            CurrentWorkspace.Tasks.TaskStarted += (s, e) => { CurrentWorkspace.Tasks.EnqueueTasksTriggeredBy(SystemEvents.TaskStarted); };
            CurrentWorkspace.Tasks.TaskFaulted += (s, e) => { CurrentWorkspace.Tasks.EnqueueTasksTriggeredBy(SystemEvents.TaskFaulted); };
            CurrentWorkspace.Tasks.TaskCompleted += (s, e) => { CurrentWorkspace.Tasks.EnqueueTasksTriggeredBy(SystemEvents.TaskCompleted); };
        }

        private List<TriggerDescriptor> ManuallyRegisteredTriggers { get; } = new List<TriggerDescriptor>();

        private List<TriggerDescriptor> Triggers =>
            CurrentWorkspace.Libraries.LoadedActionDescriptors.SelectMany(d => d.TriggeredBy)
            .Union(CurrentWorkspace.Libraries.LoadedServiceDescriptors.SelectMany(d => d.TriggeredBy))
            .Union(this.ManuallyRegisteredTriggers)
            .ToList();

        /// <summary>
        /// Register a TriggerDescriptor manually
        /// </summary>
        /// <param name="descriptor">Trigger descriptor</param>
        public void RegisterManually(TriggerDescriptor descriptor)
        {
            if (Triggers.Contains(descriptor))
                return;
            ManuallyRegisteredTriggers.Add(descriptor);
        }

        /// <summary>
        /// Get all Actions triggered by the execution of the given Action
        /// </summary>
        /// <param name="triggeringAction">Triggering Actio</param>
        /// <returns></returns>
        public List<TriggerDescriptor> GetDescriptorsByTriggeringAction(string triggeringAction) =>
            Triggers.Where(t => t.TriggerSource == Libraries.TriggerSources.Action &&
                                 t.TriggeringIdentifier == triggeringAction).ToList();

        /// <summary>
        /// Get all Actions triggered by the execution of the given system action
        /// </summary>
        /// <param name="systemEvent">System event that triggers the Action</param>
        /// <returns></returns>
        public List<TriggerDescriptor> GetDescriptorsByTriggeringSystemAction(SystemEvents systemEvent) =>
            Triggers.Where(t => t.TriggerSource == Libraries.TriggerSources.System &&
                                 t.SystemEvent == systemEvent).ToList();

        /// <summary>
        /// Get all Actions triggered by the creation or modification of a given Artifact
        /// </summary>
        /// <param name="artifact">Artifact name and path which triggers the Action</param>
        /// <returns></returns>
        public List<TriggerDescriptor> GetDescriptorsByTriggeringArtifact(Artifact artifact) =>
            Triggers.Where(t => t.TriggerSource == Libraries.TriggerSources.Artifact &&
                                 t.TriggeringIdentifier == artifact.GetAddress()).ToList();
    }
}
