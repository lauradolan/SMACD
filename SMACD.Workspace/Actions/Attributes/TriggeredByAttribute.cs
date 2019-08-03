using System;
using System.Collections.Generic;

namespace SMACD.Workspace.Actions.Attributes
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
        public ActionTriggerSources TriggerSource { get; }

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
        public TriggeredByAttribute(ActionTriggerSources source, string identifier)
        {
            TriggerSource = source;
            Identifier = identifier;
        }
    }
}
