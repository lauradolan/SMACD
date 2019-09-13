using System.Runtime.CompilerServices;
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

        // Do not allow compiler to optimize out set method, since it's only 
        //   called in reflection
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void BindCoreFeatures(ILogger logger)
        {
            Logger = logger;
        }

        public void SetLoggerName(string name)
        {
            Logger = Global.LogFactory.CreateLogger(name);
        }
    }
}