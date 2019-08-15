using System;

namespace SMACD.SDK.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class TriggeredByAttribute : Attribute
    {
        public TriggerDescriptor Trigger { get; }

        /// <summary>
        /// This Action is triggered by an operation occurring on the Artifact tree
        /// </summary>
        /// <param name="artifactPath">Artifact path</param>
        /// <param name="trigger">Trigger operation</param>
        public TriggeredByAttribute(string artifactPath, ArtifactTrigger trigger)
        {
            Trigger = TriggerDescriptor.ArtifactTrigger(artifactPath, trigger);
        }

        /// <summary>
        /// This Action is triggered by an operation occurring on the Artifact tree
        /// </summary>
        /// <param name="extensionIdentifier">Extension identifier</param>
        /// <param name="trigger">Triggering execution status</param>
        public TriggeredByAttribute(string extensionIdentifier, ExtensionConditionTrigger trigger, bool inherit = false)
        {
            Trigger = TriggerDescriptor.ExtensionTrigger(extensionIdentifier, trigger);
        }

        /// <summary>
        /// This Action is triggered by a System Event
        /// </summary>
        /// <param name="trigger">System event trigger</param>
        public TriggeredByAttribute(SystemEvents trigger)
        {
            Trigger = TriggerDescriptor.SystemEventTrigger(trigger);
        }
    }
}