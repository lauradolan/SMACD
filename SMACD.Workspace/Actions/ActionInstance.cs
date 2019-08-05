using Microsoft.Extensions.Logging;
using SMACD.Workspace.Libraries.Attributes;
using SMACD.Workspace.Targets;
using SMACD.Workspace.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SMACD.Workspace.Actions
{
    /// <summary>
    /// Base class for any Actions implemented
    /// </summary>
    public abstract class ActionInstance
    {
        /// <summary>
        /// All Targets that will be acted upon
        /// </summary>
        protected IList<TargetDescriptor> Targets =>
            GetType().GetProperties().Where(p => typeof(TargetDescriptor).IsAssignableFrom(p.PropertyType))
                .Select(p => p.GetValue(this)).Where(p => p != null).Cast<TargetDescriptor>().ToList();

        /// <summary>
        /// All Options for this Action
        /// </summary>
        protected Dictionary<string, string> Options =>
            GetType().GetProperties().Where(p => p.GetCustomAttribute<ConfigurableAttribute>() != null)
                .ToDictionary(info => info.Name, info => (string)Convert.ChangeType(info.GetValue(this), typeof(string)));

        /// <summary>
        /// Access other parts of the system
        /// </summary>
        protected Workspace Workspace { get; set; }

        /// <summary>
        /// Configuration used to set up this instance
        /// </summary>
        protected ResultProvidingTaskDescriptor RuntimeConfiguration { get; set; }

        /// <summary>
        /// Logger for this instance
        /// </summary>
        protected ILogger Logger { get; set; }

        /// <summary>
        /// Create a new instance of an Action in the system
        /// </summary>
        /// <param name="loggerName">Name to use for the logger</param>
        protected ActionInstance(string loggerName = null)
        {
            if (string.IsNullOrEmpty(loggerName))
            {
                loggerName = GetType().Name;
            }

            Logger = WorkspaceToolbox.LogFactory.CreateLogger(loggerName);
        }

        /// <summary>
        /// Execute the Action and return the Action-specific result report
        /// </summary>
        public abstract ActionSpecificReport Execute();
    }
}
