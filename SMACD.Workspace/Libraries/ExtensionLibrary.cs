using McMaster.NETCore.Plugins;
using Microsoft.Extensions.Logging;
using SMACD.Workspace.Actions;
using SMACD.Workspace.Libraries.Attributes;
using SMACD.Workspace.Services;
using SMACD.Workspace.Triggers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SMACD.Workspace.Libraries
{
    /// <summary>
    /// Represents an Assembly which contains one or more Actions or Services
    /// </summary>
    public class ExtensionLibrary
    {
        /// <summary>
        /// Name of Library that contains Actions
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Version of Library
        /// </summary>
        public Version Version { get; }

        /// <summary>
        /// Author of Library
        /// </summary>
        public string Author { get; }

        /// <summary>
        /// Website with more information about Author or Library
        /// </summary>
        public string Website { get; }

        /// <summary>
        /// Description of what the Library provides
        /// </summary>
        public string Description { get; }

        /// <summary>
        /// File name where Library was loaded from
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Assembly that represents the loaded Library in managed code
        /// </summary>
        public Assembly Assembly { get; }

        /// <summary>
        /// Actions provided by the loaded Assembly
        /// </summary>
        public List<ActionDescriptor> ActionsProvided { get; } = new List<ActionDescriptor>();

        /// <summary>
        /// Services provided by this Assembly
        /// </summary>
        public List<ServiceDescriptor> Services { get; } = new List<ServiceDescriptor>();

        /// <summary>
        /// Services running
        /// </summary>
        public Dictionary<string, ServiceInstance> RunningServices { get; } = new Dictionary<string, ServiceInstance>();

        private ILogger Logger { get; }

        /// <summary>
        /// Library File that provides Actions to the system
        /// </summary>
        /// <param name="fileName">Filename of the Library to load</param>
        public ExtensionLibrary(string fileName)
        {
            FileName = fileName;
            Logger = WorkspaceToolbox.LogFactory.CreateLogger(Path.GetFileName(fileName));

            Logger.LogDebug("Beginning to process Plugin library {0}", FileName);

            PluginLoader loader = PluginLoader.CreateFromAssemblyFile(
                FileName,
                new[]
                {
                    typeof(ActionInstance),
                    typeof(ServiceInstance),
                    typeof(ILogger),
                    typeof(Workspace),
                    typeof(Targets.TargetDescriptor),
                    typeof(Targets.HttpTarget),
                    typeof(Targets.RawPortTarget)
            });
            Assembly = loader.LoadDefaultAssembly();

            Type metadataType = Assembly.GetTypes().FirstOrDefault(t => typeof(ILibraryMetadata).IsAssignableFrom(t));
            if (metadataType == null)
            {
                Logger.LogCritical("Library does not contain metadata listing!");
                throw new Exception($"Library {FileName} built without metadata interface");
            }
            
            // -- Process Metadata --
            ILibraryMetadata metadata = (ILibraryMetadata)Activator.CreateInstance(metadataType);
            Name = metadata.Name;
            Author = metadata.Author;
            Version = metadata.Version;
            Description = metadata.Description;
            Website = metadata.Website;

            Logger.LogDebug("Loaded Library {0} v{1} by {2}", Name, Version.ToString(2), Author);

            // -- Load Actions --
            IEnumerable<Type> actionExtensions = Assembly.GetTypes().Where(t => typeof(ActionInstance).IsAssignableFrom(t));
            foreach (Type plugin in actionExtensions)
            {
                ImplementationAttribute pluginInformation = plugin.GetCustomAttribute<ImplementationAttribute>();
                if (pluginInformation == null)
                {
                    Logger.LogCritical("Action defined in {0} does not have a Implementation attribute!",
                        plugin.Name);
                    continue;
                }
                
                if (!((ActionInstance)Activator.CreateInstance(plugin)).ValidateEnvironmentReadiness())
                {
                    Logger.LogCritical("Environment readiness checks failed for Action {0}; skipping load", pluginInformation.FullIdentifier);
                    continue;
                }

                ActionsProvided.Add(new ActionDescriptor()
                {
                    FullActionId = pluginInformation.FullIdentifier,
                    ActionInstanceType = plugin,
                    TriggeredBy = GetItemsTriggering(plugin)
                });
                Logger.LogDebug("Loaded Action identifier '{0}'", pluginInformation.FullIdentifier);
            }

            // -- Load Services --
            IEnumerable<Type> serviceExtensions = Assembly.GetTypes()
                .Where(t => typeof(ServiceInstance).IsAssignableFrom(t));
            foreach (Type plugin in serviceExtensions)
            {
                ImplementationAttribute pluginInformation = plugin.GetCustomAttribute<ImplementationAttribute>();
                if (pluginInformation == null)
                {
                    Logger.LogCritical("Service defined in {0} does not have a Implementation attribute!",
                        plugin.Name);
                    continue;
                }

                var descriptor = new ServiceDescriptor()
                {
                    FullServiceId = pluginInformation.FullIdentifier,
                    ServiceInstanceType = plugin,
                    TriggeredBy = GetItemsTriggering(plugin)
                };
                
                Logger.LogDebug("Loaded Service descriptor '{0}' (plugin not yet started)", pluginInformation.FullIdentifier);
            }
        }

        public List<TriggerDescriptor> GetItemsTriggering(Type plugin) =>
             plugin.GetCustomAttributes<TriggeredByAttribute>()
                    .Where(a => a.TriggerSource == TriggerSources.Action)
                    .Select(a => new TriggerDescriptor()
                    {
                        TriggerSource = TriggerSources.Action,
                        TriggeringIdentifier = a.Identifier,
                        DefaultOptionsOnCreation = a.Options,
                        ActionIdentifierCreated = plugin.GetCustomAttribute<ImplementationAttribute>()
                                                        .FullIdentifier
                    }).ToList();

    }
}
