using Microsoft.Extensions.Logging;
using SMACD.PluginHost.Extensions;
using SMACD.PluginHost.Resources;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SMACD.PluginHost.Plugins
{
    /// <summary>
    /// Describes how to create an instance of a Plugin
    /// </summary>
    public class PluginDescription
    {
        /// <summary>
        /// Plugin Type (usage)
        /// </summary>
        public PluginTypes PluginType => PluginTypeExtensions.GetPluginType(Identifier);

        /// <summary>
        /// Partial identifier (name portion only, no type)
        /// </summary>
        public string LocalIdentifier => Identifier.Substring(Identifier.IndexOf(".") + 1);

        /// <summary>
        /// Full identifier ([type].[local identifier])
        /// </summary>
        public string Identifier { get; set; }

        /// <summary>
        /// Type of Plugin instance to create
        /// </summary>
        public Type InstanceType { get; internal set; }

        protected ILogger Logger { get; }

        internal PluginDescription(string identifier)
        {
            Identifier = identifier;
            Logger = Global.LogFactory.CreateLogger($"{PluginType}/{Identifier}");
        }

        public Plugin CreateInstance(Dictionary<string, string> options, IList<Resource> resources)
        {
            var plugin = (Plugin)Activator.CreateInstance(InstanceType,
                new object[] {WorkingDirectory.ComputeWorkingDirectory(
                    resources.FirstOrDefault()?.ResourceId,
                    Identifier,
                    options.GetHashCode())});
            plugin.PluginDescription = this;
            plugin.Configure(options, resources);
            return plugin;
        }
    }
}
