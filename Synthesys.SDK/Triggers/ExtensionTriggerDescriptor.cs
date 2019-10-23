using System;

namespace Synthesys.SDK.Triggers
{
    /// <summary>
    ///     Extension result types
    /// </summary>
    public enum ExtensionConditionTrigger
    {
        /// <summary>
        ///     Fired if the event succeeds
        /// </summary>
        Succeeds,
        
        /// <summary>
        ///     Fired if the event fails
        /// </summary>
        Fails
    }

    /// <summary>
    ///     Create a descriptor for a trigger activated by execution of an Extension
    /// </summary>
    public class ExtensionTriggerDescriptor : TriggerDescriptor
    {
        /// <summary>
        ///     Create a descriptor for a trigger activated by execution of an Extension
        /// </summary>
        /// <param name="extensionIdentifier">Artifact path</param>
        /// <param name="trigger">Triggering operation</param>
        public ExtensionTriggerDescriptor(string extensionIdentifier, ExtensionConditionTrigger trigger)
        {
            ExtensionIdentifier = extensionIdentifier;
            Trigger = trigger;
        }

        /// <summary>
        ///     Extension identifier being tracked
        /// </summary>
        public string ExtensionIdentifier { get; set; }

        /// <summary>
        ///     Trigger condition
        /// </summary>
        public ExtensionConditionTrigger Trigger { get; set; }

        /// <summary>
        ///     String representation of Extension Trigger
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"Extension Trigger ({ExtensionIdentifier} {Trigger.ToString()})";
        }

        /// <summary>
        ///     If an Extension Trigger matches another
        /// </summary>
        /// <param name="obj">Extension Trigger to compare</param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
            {
                return false;
            }

            ExtensionTriggerDescriptor castDescriptor = obj as ExtensionTriggerDescriptor;
            if (castDescriptor.ExtensionIdentifier == ExtensionIdentifier &&
                castDescriptor.Trigger == Trigger)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Get hash code from Extension Identifier and trigger condition
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(ExtensionIdentifier, Trigger);
        }
    }
}