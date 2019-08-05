using Microsoft.Extensions.Logging;
using SMACD.Workspace.Artifacts;
using SMACD.Workspace.Libraries;
using SMACD.Workspace.Tasks;
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
        public event EventHandler<TriggerDescriptor> TriggerRegistered;

        private List<TriggerDescriptor> _triggers = new List<TriggerDescriptor>();

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

        /// <summary>
        /// Register a Trigger
        /// </summary>
        /// <param name="triggerDescriptor">Item inheriting from TriggerDesc that describes the Target</param>
        public void RegisterTrigger(TriggerDescriptor triggerDescriptor)
        {
            if (_triggers.Any(t => t == triggerDescriptor))
            {
                Logger.LogWarning("Trigger already registered");
                return;
            }
            _triggers.Add(triggerDescriptor);
        }

        /// <summary>
        /// Get all Actions triggered by the execution of the given Action
        /// </summary>
        /// <param name="triggeringAction">Triggering Actio</param>
        /// <returns></returns>
        public List<TriggerDescriptor> GetDescriptorsByTriggeringAction(string triggeringAction) =>
            _triggers.Where(t => t.TriggerSource == Libraries.TriggerSources.Action &&
                                 t.TriggeringIdentifier == triggeringAction).ToList();

        /// <summary>
        /// Get all Actions triggered by the execution of the given system action
        /// </summary>
        /// <param name="systemEvent">System event that triggers the Action</param>
        /// <returns></returns>
        public List<TriggerDescriptor> GetDescriptorsByTriggeringSystemAction(SystemEvents systemEvent) =>
            _triggers.Where(t => t.TriggerSource == Libraries.TriggerSources.System &&
                                 t.SystemEvent == systemEvent).ToList();

        /// <summary>
        /// Get all Actions triggered by the creation or modification of a given Artifact
        /// </summary>
        /// <param name="artifact">Artifact name and path which triggers the Action</param>
        /// <returns></returns>
        public List<TriggerDescriptor> GetDescriptorsByTriggeringArtifact(Artifact artifact) =>
            _triggers.Where(t => t.TriggerSource == Libraries.TriggerSources.Artifact &&
                                 t.TriggeringIdentifier == artifact.GetAddress()).ToList();
    }
}
