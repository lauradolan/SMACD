using System;
using System.Collections.Generic;
using SMACD.ScannerEngine.Plugins;

namespace SMACD.ScannerEngine.Factories
{
    public class AttackToolPluginFactory : PluginFactory<AttackToolPlugin>
    {
        private static readonly Lazy<AttackToolPluginFactory> _instance =
            new Lazy<AttackToolPluginFactory>(() => new AttackToolPluginFactory());

        private AttackToolPluginFactory() : base("SMACD.Plugins.*.dll")
        {
        }

        public static AttackToolPluginFactory Instance => _instance.Value;

        /// <summary>
        /// Creates an instance of an Attack Tool with a given identifier,
        /// whose behavior is adjusted by a set of options
        /// </summary>
        /// <param name="identifier">Attack Tool identifier</param>
        /// <param name="options">Options</param>
        /// <returns></returns>
        public AttackToolPlugin Emit(string identifier, Dictionary<string, string> options)
        {
            return (AttackToolPlugin) base.Emit(identifier)
                .WithOptions(options);
        }
    }
}