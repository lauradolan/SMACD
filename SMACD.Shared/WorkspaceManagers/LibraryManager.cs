using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SMACD.Shared.WorkspaceManagers
{
    public abstract class LibraryManager<TBaseType>
    {
        private static string EXTENSION_SEARCH_PATH = Directory.GetCurrentDirectory();

        protected ILogger Logger { get; }
        public IList<Type> LoadedLibraryTypes { get; private set; } = new List<Type>();

        // Thread-safe _only_ because writes are only done at init
        public IDictionary<string, Type> RecognizedIdentifiers { get; private set; } = new Dictionary<string, Type>();

        public LibraryManager(string pluginFileMask, string loggerName)
        {
            Logger = Extensions.LogFactory.CreateLogger(loggerName);
            Logger.LogDebug("Searching directory {0} for extensions with mask {1}", EXTENSION_SEARCH_PATH, pluginFileMask);
            foreach (var file in Directory.GetFiles(EXTENSION_SEARCH_PATH, pluginFileMask))
            {
                Logger.LogDebug("Loading Assembly from {0}", file);
                var asm = Assembly.LoadFrom(file);
                var plugins = asm.GetTypes().Where(t => typeof(TBaseType).IsAssignableFrom(t));
                Logger.LogDebug("Found {0} plugins in {1}", plugins.Count(), file);
                foreach (var plugin in plugins)
                    LoadedLibraryTypes.Add(plugin);
            }

            RecognizedIdentifiers = LoadedLibraryTypes.ToDictionary(k => GetTypeIdentifier(k), v => v);

            Logger.LogInformation("Created {0} instance with {1} available extension types of base type '{2}': {3}", GetType().Name, LoadedLibraryTypes.Count, typeof(TBaseType).Name, string.Join(", ", RecognizedIdentifiers.Keys));
        }

        protected abstract string GetTypeIdentifier(Type type);

        public bool Exists(string identifier) => RecognizedIdentifiers.ContainsKey(identifier);

        public Type GetLibraryType(string identifier) => RecognizedIdentifiers.ContainsKey(identifier) ? RecognizedIdentifiers[identifier] : null;

        public TBaseType GetInstance(string identifier)
        {
            Logger.LogDebug("Creating instance of {0} type from identifier '{1}'", typeof(TBaseType).Name, identifier);
            return (TBaseType)Activator.CreateInstance(GetLibraryType(identifier));
        }

        public TBaseType GetInstance(string identifier, params object[] parameters)
        {
            Logger.LogDebug("Creating instance of {0} type from identifier '{1}' (with {2} parameter[s] given)", typeof(TBaseType).Name, identifier, parameters.Length);
            return (TBaseType)Activator.CreateInstance(GetLibraryType(identifier), parameters);
        }
    }
}