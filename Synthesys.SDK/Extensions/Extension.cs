using Microsoft.Extensions.Logging;
using Synthesys.SDK.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

        /// <summary>
        ///     Retrieve the Extension's metadata information
        /// </summary>
        public ExtensionAttribute Metadata => GetType().GetCustomAttribute<ExtensionAttribute>();

        /// <summary>
        ///     Retrieve a list of all properties in the Extension marked "Configurable"
        /// </summary>
        public List<PropertyInfo> ConfigurableProperties => GetType().GetProperties(BindingFlags.Instance).Where(p => p.GetCustomAttribute<ConfigurableAttribute>() != null).ToList();

        /// <summary>
        ///     Retrieve a dictionary of all (string) properties and their values
        /// </summary>
        public Dictionary<string, object> ConfigurablePropertyValues =>
            GetType().GetProperties(BindingFlags.Instance).Where(p => p.GetCustomAttribute<ConfigurableAttribute>() != null)
            .ToDictionary(
                k => k.Name,
                v => v.GetValue(this));

        /// <summary>
        ///     Default Summary view for this Extension.
        ///     This can be overridden by specifying an alternate view in the ExtensionReport
        /// </summary>
        public Type DefaultSummaryView => GetType().GetCustomAttribute<UseGraphicalViewsAttribute>()?.SummaryView;

        /// <summary>
        ///     Default Detail view for this Extension.
        ///     This can be overridden by specifying an alternate view in the ExtensionReport
        /// </summary>
        public Type DefaultDetailView => GetType().GetCustomAttribute<UseGraphicalViewsAttribute>()?.DetailView;
    }
}