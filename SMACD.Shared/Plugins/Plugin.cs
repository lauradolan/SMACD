using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SMACD.Shared.Attributes;
using SMACD.Shared.Data;
using SMACD.Shared.WorkspaceManagers;

namespace SMACD.Shared.Plugins
{
    /// <summary>
    ///     Represents a wrapper that can launch a scanner/tool and interpret its output to be summarized
    /// </summary>
    public abstract class Plugin
    {
        /// <summary>
        ///     Name of the plugin
        /// </summary>
        public string Name => this.GetConfigAttribute<PluginMetadataAttribute, string>(a => a.Name);

        /// <summary>
        ///     Identifier to be used in descriptor files
        /// </summary>
        public string Identifier => this.GetConfigAttribute<PluginMetadataAttribute, string>(a => a.Identifier);

        /// <summary>
        ///     Resource types that can be processed by this plugin
        /// </summary>
        public IList<Type> ValidResourceTypes =>
            this.GetConfigAttribute<ValidResourcesAttribute, List<Type>>(a => a.Types);

        /// <summary>
        ///     Options that can be specified to modify the default behavior of the plugin
        /// </summary>
        public IList<ConfigurableOptionAttribute> ConfigurableOptions => this
            .GetConfigAttributes<ConfigurableOptionAttribute, ConfigurableOptionAttribute>(a => a).ToList();

        /// <summary>
        ///     Options that can be specified to modify the default behavior of the plugin
        /// </summary>
        public double Confidence => this.GetConfigAttribute<PluginMetadataAttribute, double>(a => a.Confidence);

        /// <summary>
        ///     Logger for plugin
        /// </summary>
        protected ILogger Logger { get; set; } = Extensions.LogFactory.CreateLogger("Plugin Init");

        /// <summary>
        ///     Execute any tasks that the plugin requires to generate some output (i.e. run a scanner)
        /// </summary>
        /// <param name="pointer">Pointer to plugin and its configuration</param>
        /// <param name="workingDirectory">Working directory to store artifacts</param>
        /// <returns></returns>
        public abstract Task<PluginResult> Execute(PluginPointerModel pointer, string workingDirectory);

        /// <summary>
        ///     Reprocess result artifacts without rerunning any time-heavy scanner tasks
        /// </summary>
        /// <param name="workingDirectory">Working directory to store artifacts</param>
        /// <returns></returns>
        public abstract Task<PluginResult> Reprocess(string workingDirectory);

        /// <summary>
        ///     Validate that the settings provided in a given pointer are valid
        /// </summary>
        /// <param name="pointer">Plugin pointer to validate</param>
        /// <returns></returns>
        public bool Validate(PluginPointerModel pointer)
        {
            var coalescedOptions = GetOptions(pointer.PluginParameters);
            if (ConfigurableOptions.Any(o =>
                o.Required && (!coalescedOptions.ContainsKey(o.OptionName) ||
                               string.IsNullOrEmpty(coalescedOptions[o.OptionName]))))
            {
                Logger.LogCritical("One or more required configuration elements are missing!");
                return false;
            }

            if (ValidResourceTypes == null && pointer.Resource != null)
            {
                Logger.LogCritical("Plugin does not take any Resource inputs");
                return false;
            }

            if (pointer.Resource != null)
            {
                var targetResource = ResourceManager.Instance.GetByPointer(pointer.Resource); // Check if this resolves
                if (targetResource == null)
                {
                    Logger.LogCritical("Resource {0} does not resolve", pointer.Resource.ResourceId);
                    return false;
                }

                if (!ValidResourceTypes.Any(t => t.IsInstanceOfType(targetResource)))
                {
                    Logger.LogCritical("One or more resources are not supported by plugin");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        ///     Retrieve a Task that will execute this Plugin (inside a wrapper)
        ///     This Task will be tagged with its Plugin name
        /// </summary>
        /// <param name="pointer">Pointer that describes the Plugin and its options</param>
        /// <returns></returns>
        public Task<PluginResult> GetValidatedExecutionTask(PluginPointerModel pointer)
        {
            var logger = Extensions.LogFactory.CreateLogger<Plugin>();
            try
            {
                Validate(pointer);
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Error validating Plugin Pointer for execution");
            }

            Task<PluginResult> generatedTask = null;
            generatedTask = new Task<PluginResult>(() =>
            {
                try
                {
                    Logger = Extensions.LogFactory.CreateLogger($"{Identifier}@{pointer.Resource?.ResourceId}");

                    logger.LogDebug("Starting scheduled task -- Plugin: {0} -- Resource: {1}", pointer.Plugin,
                        pointer.Resource?.ResourceId);
                    var result = Execute(pointer, Workspace.Instance.GetChildWorkingDirectory(pointer)).Result;
                    logger.LogDebug("Completed scheduled task -- Plugin: {0} -- Resource: {1}", pointer.Plugin,
                        pointer.Resource?.ResourceId);
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
            var coalescedOptions =
                new Dictionary<string, string>(ConfigurableOptions.ToDictionary(k => k.OptionName, v => v.Default));
            foreach (var item in options)
                coalescedOptions[item.Key] = item.Value;
            return coalescedOptions;
        }
    }
}