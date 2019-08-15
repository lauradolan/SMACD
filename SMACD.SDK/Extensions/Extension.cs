using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace SMACD.SDK.Extensions
{
    public abstract class Extension
    {
        protected ILogger Logger { get; private set; }

        /// <summary>
        /// Initialize Extension; called on instantiation
        /// </summary>
        public virtual void Initialize() { }

        /// <summary>
        /// Validate the running environment is compatible with the Extension
        /// 
        /// This is called when the containing library is loaded.
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

        protected void SetLoggerName(string name)
        {
            Logger = Global.LogFactory.CreateLogger(name);
        }
    }
}
