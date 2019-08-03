using System.Collections.Generic;

namespace SMACD.Workspace.Actions
{
    /// <summary>
    /// Describes an Action that is triggered by some other action in the system
    /// </summary>
    internal class ActionTriggerDescriptor
    {
        /// <summary>
        /// Identifer of the Action to be enqueued
        /// </summary>
        internal string ActionIdentifierCreated { get; set; }

        /// <summary>
        /// Default Options to use with the Action when executed
        /// </summary>
        internal Dictionary<string, string> DefaultOptionsOnCreation { get; set; }

        /// <summary>
        /// Part of the system that produces the Trigger
        /// </summary>
        internal ActionTriggerSources TriggerSource { get; set; }

        /// <summary>
        /// Identifier of the specific item that produces the Trigger
        /// </summary>
        internal string TriggeringIdentifier { get; set; }
    }
}
