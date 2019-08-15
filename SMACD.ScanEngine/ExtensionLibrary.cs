using McMaster.NETCore.Plugins;
using Microsoft.Extensions.Logging;
using SMACD.Artifacts;
using SMACD.SDK;
using SMACD.SDK.Attributes;
using SMACD.SDK.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SMACD.ScanEngine
{
    public class ExtensionLibrary
    {
        /// <summary>
        /// File name where Library was loaded from
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Assembly that represents the loaded Library in managed code
        /// </summary>
        public Assembly Assembly { get; }

        private readonly Dictionary<string, Type> _actionExtensions = new Dictionary<string, Type>();
        private readonly Dictionary<TriggerDescriptor, List<Type>> _reactionExtensions = new Dictionary<TriggerDescriptor, List<Type>>();

        /// <summary>
        /// ActionExtensions provided by library
        /// </summary>
        public ReadOnlyDictionary<string, Type> ActionExtensions => new ReadOnlyDictionary<string, Type>(_actionExtensions);

        /// <summary>
        /// ReactionExtensions grouped by Trigger requirement
        /// </summary>
        public ReadOnlyDictionary<TriggerDescriptor, List<Type>> ReactionExtensions => new ReadOnlyDictionary<TriggerDescriptor, List<Type>>(_reactionExtensions);

        /// <summary>
        /// Types defined in the library Assembly
        /// </summary>
        public List<Type> ProvidedTypes => _actionExtensions.Values.Select(v => v)
            .Union(_reactionExtensions.Values.SelectMany(v => v)).ToList();

        protected ILogger Logger { get; } = Global.LogFactory.CreateLogger("ExtensionLibrary");

        /// <summary>
        /// Library File that provides Actions to the system
        /// </summary>
        /// <param name="fileName">Filename of the Library to load</param>
        public ExtensionLibrary(string fileName)
        {
            FileName = fileName;
            Logger = Global.LogFactory.CreateLogger(Path.GetFileName(fileName));

            Logger.LogDebug("Beginning to process Plugin library {0}", FileName);
            PluginLoader loader = PluginLoader.CreateFromAssemblyFile(
                FileName,
                new[]
                {
                    typeof(Extension),
                    typeof(Artifact),
                    typeof(ILogger)
                });
            Assembly = loader.LoadDefaultAssembly();
            PopulatePlugins();
        }

        private void PopulatePlugins()
        {
            IEnumerable<Type> allExtensions = Assembly.GetTypes().Where(t => typeof(Extension).IsAssignableFrom(t) && !t.IsAbstract);
            foreach (Type extension in allExtensions)
            {
                // Check for [Extension] metadata
                if (extension.GetCustomAttribute<ExtensionAttribute>() == null)
                {
                    Logger.LogCritical("Extension class {0} is not decorated with an [Extension] attribute", extension.Name);
                    continue;
                }

                // Load metadata
                ExtensionAttribute metadata = extension.GetCustomAttribute<ExtensionAttribute>();
                Logger.LogDebug("Loaded Extension {0} v{1} by {2}", metadata.Name, metadata.VersionObj.ToString(2), metadata.Author);

                // Create instance and validate readiness
                Extension instance = (Extension)Activator.CreateInstance(extension);
                if (!instance.ValidateEnvironmentReadiness())
                {
                    Logger.LogCritical("Extension {0} signals that the environment is incompatible with the extension; see logs for more information", metadata.ExtensionIdentifier);
                    continue;
                }

                // Ensure Triggers are specified for ReactionExtensions
                if (instance is ReactionExtension && !extension.GetCustomAttributes<TriggeredByAttribute>().Any())
                {
                    Logger.LogCritical("ReactionExtension {0} is not decorated with a [TriggeredBy<Artifact/Extension/SystemEvent>] attribute, so it will never be triggered", metadata.ExtensionIdentifier);
                    continue;
                }

                // Run Initialize
                Logger.LogDebug("Starting Initialization routine for {0}", metadata.ExtensionIdentifier);
                instance.Initialize();

                if (instance is ReactionExtension)
                {
                    Logger.LogDebug("Binding Triggers for {0} ...", metadata.ExtensionIdentifier);
                    foreach (TriggerDescriptor trigger in extension.GetCustomAttributes<TriggeredByAttribute>().Select(t => t.Trigger))
                    {
                        Logger.LogDebug("Binding Trigger {0} to {1}", trigger.ToString(), metadata.ExtensionIdentifier);
                        if (!_reactionExtensions.ContainsKey(trigger))
                        {
                            _reactionExtensions[trigger] = new List<Type>();
                        }

                        _reactionExtensions[trigger].Add(extension);
                    }
                }
                else
                {
                    Logger.LogDebug("Registering ActionExtension {0}", metadata.ExtensionIdentifier);
                    _actionExtensions.Add(metadata.ExtensionIdentifier, extension);
                }
            }
        }
    }
}
