using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using McMaster.NETCore.Plugins;
using Microsoft.Extensions.Logging;
using SMACD.Shared.Plugins;

namespace SMACD.Shared.WorkspaceManagers
{
    public abstract class LibraryManager<TBaseType>
    {
        private static readonly string EXTENSION_SEARCH_PATH = Directory.GetCurrentDirectory();

        public LibraryManager(string pluginFileMask, string loggerName)
        {
            Logger = Extensions.LogFactory.CreateLogger(loggerName);
            Logger.LogDebug("Searching directory {0} for extensions with mask {1}", EXTENSION_SEARCH_PATH,
                pluginFileMask);
            foreach (var file in Directory.GetFiles(EXTENSION_SEARCH_PATH, pluginFileMask))
            {
                Logger.LogDebug("Loading Assembly from {0}", file);

                var loader = PluginLoader.CreateFromAssemblyFile(
                    file,
                    new[] {typeof(Plugin), typeof(PluginResult), typeof(ServiceHook), typeof(ILogger)});
                var asm = loader.LoadDefaultAssembly();

                var plugins = asm.GetTypes().Where(t => typeof(TBaseType).IsAssignableFrom(t));
                Logger.LogDebug("Found {0} plugins in {1}", plugins.Count(), file);
                foreach (var plugin in plugins)
                    LoadedLibraryTypes.Add(plugin);
            }

            RecognizedIdentifiers = LoadedLibraryTypes.ToDictionary(k => GetTypeIdentifier(k), v => v);

            Logger.LogInformation("Created {0} instance with {1} available extension types of base type '{2}': {3}",
                GetType().Name, LoadedLibraryTypes.Count, typeof(TBaseType).Name,
                string.Join(", ", RecognizedIdentifiers.Keys));
        }

        protected ILogger Logger { get; }
        public IList<Type> LoadedLibraryTypes { get; } = new List<Type>();

        // Thread-safe _only_ because writes are only done at init
        public IDictionary<string, Type> RecognizedIdentifiers { get; } = new Dictionary<string, Type>();

        protected abstract string GetTypeIdentifier(Type type);

        public bool Exists(string identifier)
        {
            return RecognizedIdentifiers.ContainsKey(identifier);
        }

        public Type GetLibraryType(string identifier)
        {
            return RecognizedIdentifiers.ContainsKey(identifier) ? RecognizedIdentifiers[identifier] : null;
        }

        public TBaseType GetInstance(string identifier)
        {
            Logger.LogTrace("Creating instance of {0} type from identifier '{1}'", typeof(TBaseType).Name, identifier);
            return (TBaseType) Activator.CreateInstance(GetLibraryType(identifier));
        }

        public TBaseType GetInstance(string identifier, params object[] parameters)
        {
            Logger.LogTrace("Creating instance of {0} type from identifier '{1}' (with {2} parameter[s] given)",
                typeof(TBaseType).Name, identifier, parameters.Length);
            return (TBaseType) Activator.CreateInstance(GetLibraryType(identifier), parameters);
        }
    }
}