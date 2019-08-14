using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace SMACD.SDK.Extensions
{
    public abstract class Extension
    {
        protected ILogger Logger { get; private set; }
        public virtual void Initialize() { }
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
