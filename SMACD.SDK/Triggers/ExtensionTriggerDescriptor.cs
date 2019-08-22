using System;

namespace SMACD.SDK.Triggers
{
    public enum ExtensionConditionTrigger
    {
        Succeeds,
        Fails
    }
    public class ExtensionTriggerDescriptor : TriggerDescriptor
    {
        public string ExtensionIdentifier { get; set; }
        public ExtensionConditionTrigger Trigger { get; set; }

        /// <summary>
        /// Create a descriptor for a trigger activated by execution of an Extension
        /// </summary>
        /// <param name="extensionIdentifier">Artifact path</param>
        /// <param name="trigger">Triggering operation</param>
        public ExtensionTriggerDescriptor(string extensionIdentifier, ExtensionConditionTrigger trigger)
        {
            ExtensionIdentifier = extensionIdentifier;
            Trigger = trigger;
        }

        public override string ToString()
        {
            return $"Extension Trigger ({ExtensionIdentifier} {Trigger.ToString()})";
        }

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
            {
                return false;
            }

            ExtensionTriggerDescriptor castDescriptor = obj as ExtensionTriggerDescriptor;
            if (castDescriptor.ExtensionIdentifier == ExtensionIdentifier && castDescriptor.Trigger == Trigger)
            {
                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(ExtensionIdentifier, Trigger);
        }
    }
}
