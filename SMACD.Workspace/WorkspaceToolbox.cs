using Microsoft.Extensions.Logging;

namespace SMACD.Workspace
{
    public abstract class WorkspaceToolbox
    {
        /// <summary>
        /// Log factory
        /// </summary>
        public static ILoggerFactory LogFactory { get; } = new LoggerFactory();

        protected ILogger Logger { get; }
        protected Workspace CurrentWorkspace { get; }

        protected WorkspaceToolbox(Workspace workspace)
        {
            CurrentWorkspace = workspace;
            Logger = LogFactory.CreateLogger(GetType().Name);
        }
    }
}
