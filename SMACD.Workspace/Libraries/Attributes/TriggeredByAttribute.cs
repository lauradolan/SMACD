using System;
using System.Collections.Generic;

namespace SMACD.Workspace.Libraries.Attributes
{
    /// <summary>
    /// Denotes that the Action can be triggered by either an Artifact's creation or the completion of another Action
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class TriggeredByAttribute : Attribute
    {
        /// <summary>
        /// Source of entity which triggers the Action
        /// </summary>
        public TriggerSources TriggerSource { get; }

        /// <summary>
        /// System event which was fired (if TriggerSource is System)
        /// </summary>
        public SystemEvents SystemEvent { get; }

        /// <summary>
        /// Identifier of Action to enqueue when the Trigger is fired
        /// </summary>
        public string Identifier { get; }

        /// <summary>
        /// Options to pass to Action enqueued when the Trigger is fired
        /// </summary>
        public Dictionary<string, string> Options { get; set; }

        /// <summary>
        /// Information about the event that triggers this Action
        /// </summary>
        /// <param name="source">Trigger source</param>
        /// <param name="identifier">Identifier of triggering Action</param>
        public TriggeredByAttribute(TriggerSources source, string identifier)
        {
            if (source == TriggerSources.System)
                throw new Exception("Must specify System event with SystemEvents constructor overload");
            TriggerSource = source;
            Identifier = identifier;
        }

        /// <summary>
        /// Information about the System event that triggers this Action
        /// </summary>
        /// <param name="systemEvent">System event</param>
        public TriggeredByAttribute(SystemEvents systemEvent)
        {
            TriggerSource = TriggerSources.System;
            SystemEvent = systemEvent;
        }
    }
}
