using System;
using System.Collections.Generic;

namespace SMACD.Workspace.Actions
{
    /// <summary>
    /// Purpose of the Action in the overall system
    /// </summary>
    public enum ActionRoles
    {
        Unknown,
        Producer,
        Consumer,
        Decider
    }

    /// <summary>
    /// Sources in the system that trigger an Action
    /// </summary>
    public enum ActionTriggerSources
    {
        Action,
        Artifact
    }

    /// <summary>
    /// Describes the information needed to create an instance of an Action
    /// </summary>
    public class ActionDescriptor
    {
        /// <summary>
        /// Full Identifier for Action ({type}.{local identifier})
        /// </summary>
        public string FullActionId { get; set; }

        /// <summary>
        /// Action Type, derived from FullActionId
        /// </summary>
        public ActionRoles Type
        {
            get
            {
                if (!FullActionId.Contains('.'))
                {
                    return ActionRoles.Unknown;
                }

                if (!Enum.TryParse<ActionRoles>(FullActionId.Split('.')[0], out ActionRoles actionType))
                {
                    return ActionRoles.Unknown;
                }

                return actionType;
            }
        }

        /// <summary>
        /// Instance type to create when using this Action
        /// </summary>
        public Type ActionInstanceType { get; set; }

        /// <summary>
        /// Actions that cause this Action to be enqueued on the Task queue when the triggering Action is complete or the Artifact is created
        /// </summary>
        internal List<ActionTriggerDescriptor> TriggeredBy { get; set; } = new List<ActionTriggerDescriptor>();
    }
}
