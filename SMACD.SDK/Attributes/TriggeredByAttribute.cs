using System;

namespace SMACD.SDK.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class TriggeredByAttribute : Attribute
    {
        public TriggerDescriptor Trigger { get; }
        public bool Inherit { get; }

        public TriggeredByAttribute(string artifactPath, ArtifactTrigger trigger, bool inherit = false)
        {
            Trigger =
TriggerDescriptor.ArtifactTrigger(artifactPath, trigger, inherit);
        }

        public TriggeredByAttribute(string extensionIdentifier, ExtensionConditionTrigger trigger, bool inherit = false)
        {
            Trigger =
TriggerDescriptor.ExtensionTrigger(extensionIdentifier, trigger, inherit);
        }

        public TriggeredByAttribute(SystemEvents trigger, bool inherit = false)
        {
            Trigger =
TriggerDescriptor.SystemEventTrigger(trigger, inherit);
        }
    }
}