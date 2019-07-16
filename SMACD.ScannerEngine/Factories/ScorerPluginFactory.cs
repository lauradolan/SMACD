using System;
using System.Collections.Generic;
using SMACD.ScannerEngine.Plugins;

namespace SMACD.ScannerEngine.Factories
{
    public class ScorerPluginFactory : PluginFactory<ScorerPlugin>
    {
        private static readonly Lazy<ScorerPluginFactory> _instance =
            new Lazy<ScorerPluginFactory>(() => new ScorerPluginFactory());

        private ScorerPluginFactory() : base("SMACD.Plugins.*.dll")
        {
        }

        public static ScorerPluginFactory Instance => _instance.Value;

        public ScorerPlugin Emit(string identifier, Dictionary<string, string> options)
        {
            return (ScorerPlugin) base.Emit(identifier)
                .WithOptions(options);
        }
    }
}