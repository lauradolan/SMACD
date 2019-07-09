using System;
using System.Linq;
using SMACD.Shared.Attributes;
using SMACD.Shared.Data;
using SMACD.Shared.Plugins;

namespace SMACD.Shared.WorkspaceManagers
{
    /// <summary>
    ///     Handles scanning and mapping of plugins
    /// </summary>
    internal class PluginManager : LibraryManager<Plugin>
    {
        private static readonly Lazy<PluginManager> _instance = new Lazy<PluginManager>(() => new PluginManager());

        private PluginManager() : base("SMACD.Plugins.*.dll", "PluginManager")
        {
        }

        internal static PluginManager Instance => _instance.Value;

        protected override string GetTypeIdentifier(Type type)
        {
            return type.GetConfigAttribute<PluginMetadataAttribute, string>(a => a.Identifier);
        }

        internal bool Exists(PluginPointerModel pointer)
        {
            return Exists(pointer.Plugin);
        }

        internal Type GetLibraryType(PluginPointerModel pointer)
        {
            return LoadedLibraryTypes.FirstOrDefault(p => GetTypeIdentifier(p) == pointer.Plugin);
        }

        internal Plugin GetInstance(PluginPointerModel pointer)
        {
            return GetInstance(pointer.Plugin);
        }

        internal Plugin GetInstance(PluginPointerModel pointer, params object[] parameters)
        {
            return GetInstance(pointer.Plugin, parameters);
        }
    }
}