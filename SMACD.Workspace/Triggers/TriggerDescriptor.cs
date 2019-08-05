using SMACD.Workspace.Libraries;
using System.Collections.Generic;

namespace SMACD.Workspace.Triggers
{
    /// <summary>
    /// Describes an Action that is triggered by some other action in the system
    /// </summary>
    public class TriggerDescriptor
    {
        /// <summary>
        /// Identifer of the Action to be enqueued
        /// </summary>
        public string ActionIdentifierCreated { get; set; }

        /// <summary>
        /// Default Options to use with the Action when executed
        /// </summary>
        public Dictionary<string, string> DefaultOptionsOnCreation { get; set; }

        /// <summary>
        /// Part of the system that produces the Trigger
        /// </summary>
        public TriggerSources TriggerSource { get; set; }

        /// <summary>
        /// Identifier of the specific item that produces the Trigger
        /// </summary>
        public string TriggeringIdentifier { get; set; }

        /// <summary>
        /// System event which was fired (if TriggerSource is System)
        /// </summary>
        public SystemEvents SystemEvent { get; }
    }
}
