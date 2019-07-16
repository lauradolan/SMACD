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

        public AttackToolPlugin Emit(string identifier, Dictionary<string, string> options)
        {
            return (AttackToolPlugin) base.Emit(identifier)
                .WithOptions(options);
        }
    }
}