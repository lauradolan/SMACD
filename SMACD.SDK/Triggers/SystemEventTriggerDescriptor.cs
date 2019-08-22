using System;

namespace SMACD.SDK.Triggers
{
    public enum SystemEvents
    {
        TaskAddedToQueue,
        TaskStarted,
        TaskCompleted,
        TaskQueueDepleted
    }
    public class SystemEventTriggerDescriptor : TriggerDescriptor
    {
        /// <summary>
        /// System event trigger
        /// </summary>
        public SystemEvents SystemEvent { get; set; }

        /// <summary>
        /// Create a descriptor for a trigger activated by a System Event
        /// </summary>
        /// <param name="systemEvent">Triggering system event</param>
        public SystemEventTriggerDescriptor(SystemEvents systemEvent)
        {
            SystemEvent = systemEvent;
        }

        public override string ToString()
        {
            return $"System Event Trigger ({SystemEvent.ToString()})";
        }

        public override bool Equals(object obj)
        {
            if (!base.Equals(obj))
            {
                return false;
            }

            SystemEventTriggerDescriptor castDescriptor = obj as SystemEventTriggerDescriptor;
            if (castDescriptor.SystemEvent == SystemEvent)
            {
                return true;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(SystemEvent);
        }
    }
}
