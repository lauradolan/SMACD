using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using McMaster.NETCore.Plugins;
using Microsoft.Extensions.Logging;
using SMACD.Shared.Attributes;
using SMACD.Shared.Extensions;
using SMACD.Shared.Plugins.AttackTools;
using SMACD.Shared.Plugins.Scorers;
using SMACD.Shared.Plugins.Services;

namespace SMACD.Shared.Plugins
{
    public abstract class LibraryManager<TBaseType>
    {
        private static readonly string EXTENSION_SEARCH_PATH = Directory.GetCurrentDirectory();

        protected LibraryManager(string pluginFileMask, string loggerName)
        {
            Logger = Workspace.LogFactory.CreateLogger(loggerName);
            Logger.LogDebug("Searching directory {0} for libraries with mask {1}", EXTENSION_SEARCH_PATH,
                pluginFileMask);
            foreach (var file in Directory.GetFiles(EXTENSION_SEARCH_PATH, pluginFileMask))
            {
                Logger.LogDebug("Loading Assembly from {0}", file);

                var loader = PluginLoader.CreateFromAssemblyFile(
                    file,
                    new[] {typeof(AttackTool), typeof(Scorer), typeof(ServiceHook), typeof(ILogger)});
                var asm = loader.LoadDefaultAssembly();

                var plugins = asm.GetTypes().Where(t => typeof(TBaseType).IsAssignableFrom(t)).ToList();
                Logger.LogDebug("Found {0} '{1}' plugins in {2}", plugins.Count(), typeof(TBaseType).Name, file);

                plugins = plugins.Where(p => p.GetConfigAttribute<MetadataAttribute, MetadataAttribute>(a => a) != null)
                    .ToList();
                Logger.LogDebug("Got {0} plugins with compatible metadata; {1}", plugins.Count(),
                    string.Join(", ",
                        plugins.Select(p => p.GetConfigAttribute<MetadataAttribute, string>(a => a.Identifier))));

                foreach (var plugin in plugins)
                    LoadedLibraryTypes.Add(plugin);
            }

            RecognizedIdentifiers = LoadedLibraryTypes.ToDictionary(k => GetIdentifierFromType(k), v => v);

            Logger.LogInformation("Created {0} instance with {1} available extension types of base type '{2}': {3}",
                GetType().Name, LoadedLibraryTypes.Count, typeof(TBaseType).Name,
                string.Join(", ", RecognizedIdentifiers.Keys));
        }

        protected LibraryManager()
        {
            Logger = Workspace.LogFactory.CreateLogger(GetType().Name);
        }

        protected ILogger Logger { get; }
        public IList<Type> LoadedLibraryTypes { get; } = new List<Type>();

        // Thread-safe _only_ because writes are only done at init
        public IDictionary<string, Type> RecognizedIdentifiers { get; } = new Dictionary<string, Type>();

        protected string GetIdentifierFromType(Type type)
        {
            return type.GetConfigAttribute<MetadataAttribute, string>(a => a.Identifier);
        }

        public bool Exists(string identifier)
        {
            return RecognizedIdentifiers.ContainsKey(identifier);
        }

        public Type GetTypeFromIdentifier(string identifier)
        {
            return RecognizedIdentifiers.ContainsKey(identifier) ? RecognizedIdentifiers[identifier] : null;
        }

        public TBaseType GetInstance(string identifier)
        {
            Logger.LogTrace("Creating instance of {0} type from identifier '{1}'", typeof(TBaseType).Name, identifier);
            try
            {
                return (TBaseType) Activator.CreateInstance(GetTypeFromIdentifier(identifier));
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Error creating instance of {0} with identifier '{1}'", typeof(TBaseType),
                    identifier);
                return default;
            }
        }

        public TBaseType GetInstance(string identifier, params object[] parameters)
        {
            Logger.LogTrace("Creating instance of {0} type from identifier '{1}' (with {2} parameter[s] given)",
                typeof(TBaseType).Name, identifier, parameters.Length);
            try
            {
                return (TBaseType) Activator.CreateInstance(GetTypeFromIdentifier(identifier), parameters);
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Error creating instance of {0} with identifier '{1}'", typeof(TBaseType),
                    identifier);
                return default;
            }
        }
    }
}