using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using McMaster.NETCore.Plugins;
using Microsoft.Extensions.Logging;
using SMACD.Data;
using SMACD.ScannerEngine.Attributes;
using SMACD.ScannerEngine.Plugins;

namespace SMACD.ScannerEngine.Factories
{
    public abstract class PluginFactory<TEmitted> where TEmitted : Plugin
    {
        private static readonly string EXTENSION_SEARCH_PATH = Directory.GetCurrentDirectory();

        public PluginFactory(string pluginFileMask)
        {
            Logger = Global.LogFactory.CreateLogger(GetType().Name);
            foreach (var file in Directory.GetFiles(EXTENSION_SEARCH_PATH, pluginFileMask))
            {
                var loader = PluginLoader.CreateFromAssemblyFile(
                    file,
                    new[]
                    {
                        typeof(AttackToolPlugin), typeof(ScorerPlugin), typeof(ServicePlugin), typeof(Plugin),
                        typeof(PluginPointerModel), typeof(ILogger)
                    });
                var asm = loader.LoadDefaultAssembly();

                var plugins = asm.GetTypes().Where(t =>
                    typeof(TEmitted).IsAssignableFrom(t) && PluginMetadataAttribute.Get(t) != null).ToList();
                plugins.ForEach(p => LoadedPluginTypes.TryAdd(PluginMetadataAttribute.Get(p).Identifier, p));
            }
        }

        public ConcurrentDictionary<string, Type> LoadedPluginTypes { get; } = new ConcurrentDictionary<string, Type>();
        protected ILogger Logger { get; set; }

        public Type ResolveIdentifier(string identifier)
        {
            return LoadedPluginTypes[identifier];
        }

        public TEmitted Emit(string identifier)
        {
            return Emit(identifier, new object[0]);
        }

        protected TEmitted Emit(string identifier, params object[] parameters)
        {
            if (!LoadedPluginTypes.ContainsKey(identifier))
            {
                Logger.LogCritical("Plugin with identifier '{0}' not found", identifier);
                throw new Exception("Plugin load error");
            }

            var pluginType =
                LoadedPluginTypes.FirstOrDefault(p => p.Key.Equals(identifier, StringComparison.OrdinalIgnoreCase));
            return (TEmitted) Activator.CreateInstance(pluginType.Value, parameters);
        }
    }
}