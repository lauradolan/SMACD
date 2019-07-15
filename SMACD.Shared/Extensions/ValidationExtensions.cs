using System;
using System.Collections.Generic;
using System.Linq;
using SMACD.Shared.Data;
using SMACD.Shared.Plugins;
using SMACD.Shared.Plugins.AttackTools;
using SMACD.Shared.Plugins.Scorers;
using SMACD.Shared.Resources;

namespace SMACD.Shared.Extensions
{
    public static class ValidationExtensions
    {
        public static Plugin Validate(this PluginPointerModel model)
        {
            IsPluginValid(model.Plugin, out var plugin);
            IsScorerValid(plugin.DefaultScorer, model.Scorer, model, out var scorer);
            DoOptionsFit(plugin, model);

            if (model.Resource != null)
            {
                var resource = model.Resource.Validate();
                if (plugin is AttackTool)
                    DoesResourceFitAttackTool((AttackTool) plugin, resource);
            }

            return plugin;
        }
        public static Resource Validate(this ResourcePointerModel model)
        {
            IsResourceValid(model.ResourceId, out var resource);
            // TODO: Validate Resource params

            return resource;
        }

        private static void IsPluginValid(string plugin) => IsPluginValid(plugin, out _);
        private static void IsPluginValid(string plugin, out AttackTool pluginInstance)
        {
            if (string.IsNullOrEmpty(plugin))
                throw new Exception("Plugin not specified");

            pluginInstance = AttackToolManager.Instance.GetInstance(plugin);
            if (pluginInstance == null)
                throw new Exception("Plugin not loaded");
        }

        private static void IsScorerValid(string defaultScorer, string scorer, PluginPointerModel pointer) => IsScorerValid(defaultScorer, scorer, pointer, out _);
        private static void IsScorerValid(string defaultScorer, string scorer, PluginPointerModel pointer, out Scorer scorerInstance)
        {
            if (string.IsNullOrEmpty(defaultScorer) &&
                string.IsNullOrEmpty(scorer))
                throw new Exception("Default scorer not provided and custom scorer not specified");

            scorerInstance = ScorerPluginManager.Instance.GetInstance(
                !string.IsNullOrEmpty(scorer) ?
                    scorer : defaultScorer, AttackToolManager.Instance.GetChildWorkingDirectory(pointer));
            if (scorerInstance == null)
                throw new Exception("Scorer not loaded");
        }

        private static void IsResourceValid(string resource) => IsResourceValid(resource, out _);
        private static void IsResourceValid(string resource, out Resource resourceInstance)
        {
            if (resource == null)
            {
                resourceInstance = null;
                return;
            }

            resourceInstance = ResourceManager.Instance.GetById(resource);
            if (resourceInstance == null)
                throw new Exception("Resource does not exist on maps");
        }

        private static void DoesResourceFitAttackTool(string attackTool, string resource) =>
            DoesResourceFitAttackTool(
                AttackToolManager.Instance.GetInstance(attackTool),
                ResourceManager.Instance.GetById(resource));
        private static void DoesResourceFitAttackTool(AttackTool tool, Resource resource)
        {
            if (!tool.ValidResourceTypes[0].IsInstanceOfType(resource))
                throw new Exception("One or more resources are not supported by plugin");
        }

        private static void DoOptionsFit(Plugin plugin, PluginPointerModel pointer)
        {
            if (pointer == null) pointer = new PluginPointerModel();
            var coalescedOptions =
                new Dictionary<string, string>(plugin.ConfigurableOptions.ToDictionary(k => k.OptionName, v => v.Default));
            foreach (var item in pointer.PluginParameters)
                coalescedOptions[item.Key] = item.Value;

            if (plugin.ConfigurableOptions.Any(o =>
                o.Required && (!coalescedOptions.ContainsKey(o.OptionName) ||
                               string.IsNullOrEmpty(coalescedOptions[o.OptionName]))))
                throw new Exception("One or more required configuration elements are missing!");
        }
    }
}
