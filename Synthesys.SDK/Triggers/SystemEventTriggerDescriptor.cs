using System;

namespace Synthesys.SDK.Triggers
{
    /// <summary>
    ///     Events which can be fired from the scanner system
    /// </summary>
    public enum SystemEvents
    {
        /// <summary>
        ///     Fired when a task is added to the queue
        /// </summary>
        TaskAddedToQueue,

        /// <summary>
        ///     Fired when a task is started
        /// </summary>
        TaskStarted,

        /// <summary>
        ///     Fired when a task completes
        /// </summary>
        TaskCompleted,

        /// <summary>
        ///     Fired when the task queue has completely drained
        /// </summary>
        TaskQueueDepleted
    }

    /// <summary>
    ///     Descriptor for a trigger activated by a System Event
    /// </summary>
    public class SystemEventTriggerDescriptor : TriggerDescriptor
    {
        /// <summary>
        ///     Create a descriptor for a trigger activated by a System Event
        /// </summary>
        /// <param name="systemEvent">Triggering system event</param>
        public SystemEventTriggerDescriptor(SystemEvents systemEvent)
        {
            SystemEvent = systemEvent;
        }

        /// <summary>
        ///     System event trigger
        /// </summary>
        public SystemEvents SystemEvent { get; set; }

        /// <summary>
        ///     String representation of System Event trigger
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"System Event Trigger ({SystemEvent.ToString()})";
        }

        /// <summary>
        ///     If a System Event trigger matches another
        /// </summary>
        /// <param name="obj">System Event Trigger to compare</param>
        /// <returns></returns>
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

        /// <summary>
        ///     Get hash code from the system event
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(SystemEvent);
        }
    }
}