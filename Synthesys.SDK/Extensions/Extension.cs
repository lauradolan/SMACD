using Microsoft.Extensions.Logging;

namespace Synthesys.SDK.Extensions
{
    /// <summary>
    ///     An Extension is some function, which can either be an Action or a Reaction, which executes with the intent of
    ///     populating the Artifact Tree with additional data.
    /// </summary>
    public abstract class Extension
    {
        protected ILogger Logger { get; private set; }

        /// <summary>
        ///     Initialize Extension; called on instantiation
        /// </summary>
        public virtual void Initialize()
        {
        }

        /// <summary>
        ///     Called when the Extension is loaded, to check if the runtime environment supports what the Extension requires to
        ///     execute.
        ///     Any application validation/dependency checks should happen here, but it is not required.
        /// </summary>
        /// <returns></returns>
        public virtual bool ValidateEnvironmentReadiness()
        {
            return true;
        }

        /// <summary>
        ///     Create a new Logger with a given name for this Extension
        /// </summary>
        /// <param name="name">Logger name</param>
        public void SetLoggerName(string name)
        {
            Logger = Global.LogFactory.CreateLogger(name);
        }
    }
}