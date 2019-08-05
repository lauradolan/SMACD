using SMACD.Workspace.Actions;
using SMACD.Workspace.Libraries;
using SMACD.Workspace.Triggers;
using System;
using System.Collections.Generic;

namespace SMACD.Workspace.Services
{
    /// <summary>
    /// Describes the information needed to create an instance of a Service
    /// </summary>
    public class ServiceDescriptor
    {
        /// <summary>
        /// Full Identifier for Service (service.{local identifier})
        /// </summary>
        public string FullServiceId { get; set; }

        /// <summary>
        /// Action Type, derived from FullActionId
        /// </summary>
        public ExtensionRoles Type
        {
            get
            {
                if (!FullServiceId.Contains('.'))
                {
                    return ExtensionRoles.Unknown;
                }

                if (!Enum.TryParse<ExtensionRoles>(FullServiceId.Split('.')[0], out ExtensionRoles actionType))
                {
                    return ExtensionRoles.Unknown;
                }

                return actionType;
            }
        }

        /// <summary>
        /// Instance type to create when using this Action
        /// </summary>
        public Type ServiceInstanceType { get; set; }

        /// <summary>
        /// Actions that cause this Action to be enqueued on the Task queue when the triggering Action is complete or the Artifact is created
        /// </summary>
        internal List<TriggerDescriptor> TriggeredBy { get; set; } = new List<TriggerDescriptor>();
    }
}
