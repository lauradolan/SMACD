using Synthesys.SDK.Triggers;
using System;

namespace Synthesys.SDK.Attributes
{
    /// <summary>
    ///     The TriggeredByAttribute specifies that the Extension is added to the end of the Task Queue when a certain event
    ///     occurs.
    ///     This Attribute can be used multiple times on the same Extension.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class TriggeredByAttribute : Attribute
    {
        /// <summary>
        ///     Specify that the Extension is added to the end of the Task Queue when an Artifact is created or changed
        /// </summary>
        /// <param name="artifactPath">Artifact path</param>
        /// <param name="trigger">Trigger event</param>
        public TriggeredByAttribute(string artifactPath, AppTreeNodeEvents trigger)
        {
            Trigger = TriggerDescriptor.ArtifactTrigger(artifactPath, trigger);
        }

        /// <summary>
        ///     Specify that the Extension is added to the end of the Task Queue when an Extension completes
        /// </summary>
        /// <param name="extensionIdentifier">Extension identifier</param>
        /// <param name="trigger">Triggering execution status</param>
        public TriggeredByAttribute(string extensionIdentifier, ExtensionConditionTrigger trigger)
        {
            Trigger = TriggerDescriptor.ExtensionTrigger(extensionIdentifier, trigger);
        }

        /// <summary>
        ///     Specify that the Extension is added to the end of the Task Queue when a system-level event occurs
        /// </summary>
        /// <param name="trigger">System event Trigger</param>
        public TriggeredByAttribute(SystemEvents trigger)
        {
            Trigger = TriggerDescriptor.SystemEventTrigger(trigger);
        }

        /// <summary>
        ///     Description of Trigger causing the Extension to be queued
        /// </summary>
        public TriggerDescriptor Trigger { get; }
    }
}