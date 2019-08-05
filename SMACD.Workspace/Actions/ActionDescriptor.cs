﻿using SMACD.Workspace.Libraries;
using SMACD.Workspace.Triggers;
using System;
using System.Collections.Generic;

namespace SMACD.Workspace.Actions
{
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
        public ExtensionRoles Type
        {
            get
            {
                if (!FullActionId.Contains('.'))
                {
                    return ExtensionRoles.Unknown;
                }

                if (!Enum.TryParse<ExtensionRoles>(FullActionId.Split('.')[0], out ExtensionRoles actionType))
                {
                    return ExtensionRoles.Unknown;
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
        internal List<TriggerDescriptor> TriggeredBy { get; set; } = new List<TriggerDescriptor>();
    }
}
