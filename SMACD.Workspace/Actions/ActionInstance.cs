using Microsoft.Extensions.Logging;
using SMACD.Workspace.Tasks;
using System;
using System.Collections.Generic;

namespace SMACD.Workspace.Actions
{
    /// <summary>
    /// Base class for any Actions implemented
    /// </summary>
    public abstract class ActionInstance
    {
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
