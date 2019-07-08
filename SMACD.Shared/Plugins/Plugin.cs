using Microsoft.Extensions.Logging;
using SMACD.Shared.Attributes;
using SMACD.Shared.Data;
using SMACD.Shared.Resources;
using SMACD.Shared.WorkspaceManagers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SMACD.Shared.Plugins
{
    /// <summary>
    /// Represents a wrapper that can launch a scanner/tool and interpret its output to be summarized
    /// </summary>
    public abstract class Plugin
    {
        /// <summary>
        /// Name of the plugin
        /// </summary>
        public string Name => this.GetConfigAttribute<PluginMetadataAttribute, string>(a => a.Name);

        /// <summary>
        /// Identifier to be used in descriptor files
        /// </summary>
        public string Identifier => this.GetConfigAttribute<PluginMetadataAttribute, string>(a => a.Identifier);

        /// <summary>
        /// Resource types that can be processed by this plugin
        /// </summary>
        public IList<Type> ValidResourceTypes => this.GetConfigAttribute<ValidResourcesAttribute, List<Type>>(a => a.Types);

        /// <summary>
        /// Options that can be specified to modify the default behavior of the plugin
        /// </summary>
        public IList<ConfigurableOptionAttribute> ConfigurableOptions => this.GetConfigAttributes<ConfigurableOptionAttribute, ConfigurableOptionAttribute>(a => a).ToList();

        /// <summary>
        /// Options that can be specified to modify the default behavior of the plugin
        /// </summary>
        public double Confidence => this.GetConfigAttribute<PluginMetadataAttribute, double>(a => a.Confidence);

        /// <summary>
        /// Logger for plugin
        /// </summary>
        protected ILogger Logger { get; private set; } = Extensions.LogFactory.CreateLogger("Plugin Init");

        public Plugin()
        {
        }

        /// <summary>
        /// Execute any tasks that the plugin requires to generate some output (i.e. run a scanner)
        /// </summary>
        /// <param name="pointer">Pointer to plugin and its configuration</param>
        /// <param name="workingDirectory">Working directory to store artifacts</param>
        /// <returns></returns>
        public abstract Task<PluginResult> Execute(PluginPointerModel pointer, string workingDirectory);

        /// <summary>
        /// Reprocess result artifacts without rerunning any time-heavy scanner tasks
        /// </summary>
        /// <param name="workingDirectory">Working directory to store artifacts</param>
        /// <returns></returns>
        public abstract Task<PluginResult> Reprocess(string workingDirectory);

        /// <summary>
        /// Retrieve a Task that will execute this Plugin (inside a wrapper)
        /// This Task will be tagged with its Plugin name and Workspace ID
        /// </summary>
        /// <param name="workspace">Workspace running this Plugin</param>
        /// <param name="pointer">Pointer that describes the Plugin and its options</param>
        /// <returns></returns>
        public Task<PluginResult> GetValidatedExecutionTask(Workspace workspace, PluginPointerModel pointer)
        {
            var logger = Extensions.LogFactory.CreateLogger<Plugin>();

            var coalescedOptions = GetOptions(pointer.PluginParameters);
            if (ConfigurableOptions.Any(o => o.Required && (!coalescedOptions.ContainsKey(o.OptionName) || string.IsNullOrEmpty(coalescedOptions[o.OptionName]))))
                throw new Exception("One or more required configuration elements are missing!");

            if (ValidResourceTypes == null && pointer.Resource != null)
                throw new Exception("Plugin does not take any Resource inputs");

            if (pointer.Resource != null)
            {
                Resource targetResource = ResourceManager.Instance.GetByPointer(pointer.Resource); // Check if this resolves
                if (targetResource == null)
                    throw new Exception("Resource does not resolve");
                if (!ValidResourceTypes.Any(t => t.IsAssignableFrom(targetResource.GetType())))
                    throw new Exception("One or more resources are not supported by plugin");
            }

            Task<PluginResult> generatedTask = null;
            generatedTask = new Task<PluginResult>(() =>
            {
                try
                {
                    this.Logger = Extensions.LogFactory.CreateLogger($"{Identifier}@{pointer.Resource?.ResourceId}");

                    logger.LogDebug("Starting scheduled task -- Plugin: {0} -- Resource: {1}", pointer.Plugin, pointer.Resource?.ResourceId);
                    var result = Execute(pointer, Workspace.Instance.GetChildWorkingDirectory(pointer)).Result;
                    logger.LogDebug("Completed scheduled task -- Plugin: {0} -- Resource: {1}", pointer.Plugin, pointer.Resource?.ResourceId);
                    return result;
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "Exception thrown while executing scheduled task");
                    return null;
                }
            });
            return generatedTask;
        }

        protected string GetOption(IDictionary<string, string> options, string option)
        {
            var opts = GetOptions(options);
            if (!opts.ContainsKey(option)) return null;
            return opts[option];
        }

        private Dictionary<string, string> GetOptions(IDictionary<string, string> options)
        {
            var coalescedOptions = new Dictionary<string, string>(ConfigurableOptions.ToDictionary(k => k.OptionName, v => v.Default));
            foreach (var item in options)
                coalescedOptions[item.Key] = item.Value;
            return coalescedOptions;
        }
    }
}