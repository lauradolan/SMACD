using McMaster.NETCore.Plugins;
using Microsoft.Extensions.Logging;
using SMACD.PluginHost.Attributes;
using SMACD.PluginHost.Reports;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SMACD.PluginHost.Plugins
{
    /// <summary>
    /// Describes a library (SMACD.Plugins.*.dll) that registers one or more PluginActions
    ///
    /// Note that the library will not automatically populate available plugins on instantiation.
    /// </summary>
    public class PluginLibrary
    {
        #region Plugin Scanning/Loading
        private const string LIBRARY_FILE_MASK = "SMACD.Plugins.*.dll";

        /// <summary>
        /// All Plugins that have been loaded
        /// </summary>
        public static List<PluginLibrary> LoadedLibraries { get; } = new List<PluginLibrary>();

        private static ILogger StaticLogger { get; } = Global.LogFactory.CreateLogger("Plugin Loader");

        static PluginLibrary()
        {
            StaticLogger.LogDebug("Loading plugins from {0} with mask {1}", Directory.GetCurrentDirectory(),
                LIBRARY_FILE_MASK);
            Directory.GetFiles(Path.Combine(Directory.GetCurrentDirectory(), "Plugins"), "SMACD.Plugins.*.dll").ToList()
                .ForEach(f => LoadedLibraries.Add(new PluginLibrary(f)));
        }
        #endregion

        /// <summary>
        /// All Plugins available based on the loaded Plugin Library
        /// </summary>
        public static Dictionary<string, PluginDescription> PluginsAvailable =>
            PluginLibrary.LoadedLibraries.SelectMany(p => p.PluginsProvided).ToDictionary(
                k => k.Identifier,
                v => v);

        private ILogger Logger { get; } = Global.LogFactory.CreateLogger("Plugin ?");

        /// <summary>
        /// Plugins that are provided by this Library
        /// </summary>
        public List<PluginDescription> PluginsProvided { get; } = new List<PluginDescription>();

        /// <summary>
        /// Name of the Plugin library
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// User or organization who created the Plugin library
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Plugin library version
        /// </summary>
        public Version Version { get; set; }

        /// <summary>
        /// Description of what the Plugin library's actions do
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Website for more information about the Plugin library or Author
        /// </summary>
        public string Website { get; set; }

        /// <summary>
        /// C# Assembly providing Plugins from this library
        /// </summary>
        public Assembly Assembly { get; }

        /// <summary>
        /// Path to Plugin library
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Describes a library (SMACD.Plugins.*.dll) that registers one or more PluginActions
        ///
        /// Note that the library will not automatically populate available plugins on instantiation.
        /// </summary>
        /// <param name="fileName">File name of library being wrapped</param>
        public PluginLibrary(string fileName)
        {
            FileName = fileName;
            Logger = Global.LogFactory.CreateLogger(Path.GetFileName(fileName));

            Logger.LogDebug("Beginning to process Plugin library {0}", FileName);

            var loader = PluginLoader.CreateFromAssemblyFile(
                FileName,
                new[]
                {
                    typeof(Plugin), typeof(PluginDescription), typeof(ScoredResult), typeof(ILogger)
                });
            Assembly = loader.LoadDefaultAssembly();

            var metadataType = Assembly.GetTypes().FirstOrDefault(t => typeof(ILibraryMetadata).IsAssignableFrom(t));
            if (metadataType == null)
            {
                Logger.LogCritical("Library does not contain metadata listing!");
                throw new Exception($"Library {FileName} built without metadata interface");
            }

            var metadata = (ILibraryMetadata)Activator.CreateInstance(metadataType);
            Name = metadata.Name;
            Author = metadata.Author;
            Version = metadata.Version;
            Description = metadata.Description;
            Website = metadata.Website;

            Logger.LogDebug("Loaded Library {0} v{1} by {2}", Name, Version.ToString(2), Author);

            var plugins = Assembly.GetTypes().Where(t => typeof(Plugin).IsAssignableFrom(t));
            foreach (var plugin in plugins)
            {
                var pluginInformation = plugin.GetCustomAttribute<PluginImplementationAttribute>();
                if (pluginInformation == null)
                {
                    Logger.LogCritical("Plugin defined in {0} does not have a PluginImplementation attribute!", plugin.Name);
                    continue;
                }
                PluginsProvided.Add(new PluginDescription(pluginInformation.FullIdentifier) { InstanceType = plugin });
                Logger.LogDebug("Loaded Plugin identifier '{0}'", pluginInformation.FullIdentifier);
            }
        }
    }
}
