using Microsoft.Extensions.Logging;
using SMACD.PluginHost.Attributes;
using SMACD.PluginHost.Extensions;
using SMACD.PluginHost.Reports;
using SMACD.PluginHost.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SMACD.PluginHost.Plugins
{
    public abstract class Plugin
    {
        private PluginDescription _pluginDescription;

        protected Plugin(string workingDirectory)
        {
            WorkingDirectory = new PluginWorkingDirectory(workingDirectory);
        }

        public IList<Resource> Resources =>
            GetType().GetProperties().Where(p => typeof(Resource).IsAssignableFrom(p.PropertyType))
                .Select(p => p.GetValue(this)).Where(p => p != null).Cast<Resource>().ToList();

        public Dictionary<string, string> Options =>
            GetType().GetProperties().Where(p => p.GetCustomAttribute<ConfigurableAttribute>() != null)
                .ToDictionary(info => info.Name, info => (string)Convert.ChangeType(info.GetValue(this), typeof(string)));

        protected ILogger Logger { get; set; } = Global.LogFactory.CreateLogger("Plugin ?");

        /// <summary>
        ///     Plugin that created this Plugin instance
        /// </summary>
        public PluginDescription PluginDescription
        {
            get => _pluginDescription;
            set
            {
                _pluginDescription = value;
                if (_pluginDescription != null)
                {
                    if (Resources.Any())
                        Logger = Global.LogFactory.CreateLogger(
                            _pluginDescription.Identifier + "@" +
                            string.Join(",", Resources.Select(r => r.ResourceId)));
                    else
                        Logger = Global.LogFactory.CreateLogger(_pluginDescription.Identifier);
                }
                else
                {
                    Logger = Global.LogFactory.CreateLogger("Unknown Plugin");
                }
            }
        }

        public PluginWorkingDirectory WorkingDirectory { get; }

        public abstract ScoredResult Execute();

        /// <summary>
        ///     Configure this Plugin by projecting provided Options and Resources to Properties associated with the items
        /// </summary>
        /// <param name="options">Plugin Options</param>
        /// <param name="resources">Plugin Resources</param>
        public void Configure(Dictionary<string, string> options, IList<Resource> resources)
        {
            var propertyTargets = GetType().GetProperties();
            foreach (var pair in options)
            {
                // Look for the [Configurable] attribute on the property and set the property from the options dictionary if it was found
                var propertyIsConfigurable =
                    propertyTargets.FirstOrDefault(t => t.Name.Equals(pair.Key, StringComparison.OrdinalIgnoreCase) &&
                                                        t.GetCustomAttribute<ConfigurableAttribute>() != null);
                if (propertyIsConfigurable != null)
                {
                    Logger.LogInformation("Found Configurable Property {0} (Type: {1}) -- Setting value {2}",
                        propertyIsConfigurable.Name, propertyIsConfigurable.PropertyType.Name, pair.Value);
                    propertyIsConfigurable.SetValue(this,
                        Convert.ChangeType(pair.Value, propertyIsConfigurable.PropertyType));
                }
                else
                {
                    Logger.LogWarning("Specified option '{0}' which is not understood by plugin '{1}'", pair.Key,
                        PluginDescription.Identifier);
                }
            }

            foreach (var property in propertyTargets.Where(t => typeof(Resource).IsAssignableFrom(t.PropertyType)))
            {
                Logger.LogDebug("Found Resource eligible target {0} with discrete Type {1}", property.Name,
                    property.PropertyType);
                var assigned = resources.FirstOrDefault(r => r.GetType() == property.PropertyType);
                if (assigned == null)
                    Logger.LogDebug("Resource eligible target {0} did not match any provided Resources", property.Name);
                else
                    property.SetValue(this, assigned);
            }
        }

        /// <summary>
        ///     Get a blank scoring template which will automatically bind this scoring to the most recent Attack Tool for this
        ///     Resource
        ///     This method will generally only be run by Scorer Plugins.
        /// </summary>
        /// <returns></returns>
        protected ScoredResult CreateBlankScoredResult()
        {
            var recent = WorkingDirectory.ParentResource.Configuration.Plugins.LastOrDefault(config =>
                PluginLibrary.PluginsAvailable[config.Identifier].PluginType == PluginTypes.AttackTool);
            return new ScoredResult
            {
                Plugin = recent,
                Scorer = new PluginSummary
                {
                    Identifier = PluginDescription.Identifier,
                    Options = Options,
                    ResourceIds = Resources.Select(r => r.ResourceId).ToList()
                }
            };
        }

        /// <summary>
        ///     Create a blank scoring template to score the artifacts generated by a given Plugin
        ///     This method will generally only be run by Scorer Plugins.
        /// </summary>
        /// <param name="plugin">Plugin whose artifacts are to be scored</param>
        /// <returns></returns>
        protected ScoredResult CreateBlankScoredResult(PluginDescription plugin, Dictionary<string, string> options,
            IList<Resource> resources)
        {
            return new ScoredResult
            {
                Plugin = new PluginSummary
                {
                    Identifier = plugin.Identifier,
                    Options = options,
                    ResourceIds = resources.Select(r => r.ResourceId).ToList()
                },
                Scorer = new PluginSummary
                {
                    Identifier = PluginDescription.Identifier,
                    Options = Options,
                    ResourceIds = Resources.Select(r => r.ResourceId).ToList()
                }
            };
        }

        /// <summary>
        ///     Allow the runtime to specify a scorer. Preferred scorer takes precedence;
        ///     otherwise a scorer named "score.(this plugin identifier)" is used
        /// </summary>
        /// <param name="preferredScorer"></param>
        /// <returns></returns>
        protected ScoredResult GetScoredResult(string preferredScorer = null)
        {
            // TODO: Include Service Map override for Scorer
            if (preferredScorer == null)
                preferredScorer = $"score.{PluginDescription.LocalIdentifier}";
            var task = TaskManager.Instance.Enqueue(new PluginSummary
            {
                Identifier = preferredScorer,
                ResourceIds = Resources.Select(r => r.ResourceId).ToList(),
                Options = Options
            });
            return task.Result;
        }
    }
}