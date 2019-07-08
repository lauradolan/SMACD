using SMACD.Shared.Attributes;
using SMACD.Shared.Data;
using SMACD.Shared.Plugins;
using System;
using System.Linq;

namespace SMACD.Shared.WorkspaceManagers
{
    /// <summary>
    /// Handles scanning and mapping of plugins
    /// </summary>
    internal class PluginManager : LibraryManager<Plugin>
    {
        private static readonly Lazy<PluginManager> _instance = new Lazy<PluginManager>(() => new PluginManager());
        internal static PluginManager Instance => _instance.Value;

        private PluginManager() : base("SMACD.Plugins.*.dll", "PluginManager")
        {
        }

        protected override string GetTypeIdentifier(Type type) => type.GetConfigAttribute<PluginMetadataAttribute, string>(a => a.Identifier);

        internal bool Exists(PluginPointerModel pointer) => Exists(pointer.Plugin);

        internal Type GetLibraryType(PluginPointerModel pointer) => LoadedLibraryTypes.FirstOrDefault(p => GetTypeIdentifier(p) == pointer.Plugin);

        internal Plugin GetInstance(PluginPointerModel pointer) => (Plugin)((ILibraryManager)this).GetInstance(pointer.Plugin);

        internal Plugin GetInstance(PluginPointerModel pointer, params object[] parameters) => (Plugin)((ILibraryManager)this).GetInstance(pointer.Plugin, parameters);
    }
}