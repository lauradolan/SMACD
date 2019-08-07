using Microsoft.Extensions.Logging;
using SMACD.Workspace.Actions;
using System;

namespace SMACD.Workspace.Services
{
    /// <summary>
    /// Base class for any Services implemented
    /// </summary>
    public abstract class ServiceInstance
    {
        /// <summary>
        /// Access other parts of the system
        /// </summary>
        protected Workspace Workspace { get; set; }

        /// <summary>
        /// Logger for this instance
        /// </summary>
        protected ILogger Logger { get; set; }

        /// <summary>
        /// Create a new instance of a Service in the system
        /// </summary>
        /// <param name="loggerName">Name to use for the logger</param>
        protected ServiceInstance(string loggerName = null)
        {
            if (string.IsNullOrEmpty(loggerName))
            {
                loggerName = GetType().Name;
            }

            Logger = WorkspaceToolbox.LogFactory.CreateLogger(loggerName);
        }

        public abstract void Initialize();

        /// <summary>
        /// Validate that the running environment supports the Action; this could involve
        ///   ensuring certain applications (such as Docker) are installed and available
        /// </summary>
        /// <returns></returns>
        public virtual bool ValidateEnvironmentReadiness() => true;
    }

    public class SystemEventPalette
    {
        public event EventHandler<Tasks.ResultProvidingTaskDescriptor> TaskStarted;
        public event EventHandler<Tasks.ResultProvidingTaskDescriptor> TaskCompleted;

        public event EventHandler<Targets.TargetDescriptor> TargetAdded;


        private Workspace _workspace;
        internal SystemEventPalette(Workspace workspace)
        {
            _workspace = workspace;
            _workspace.Tasks.TaskStarted += (s, e) => TaskStarted?.Invoke(s, e);
            _workspace.Tasks.TaskCompleted += (s, e) => TaskCompleted?.Invoke(s, e);

            _workspace.Targets.TargetRegistered += (s, e) => TargetAdded?.Invoke(s, e);
        }
    }
}
 