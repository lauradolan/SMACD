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

        /// <summary>
        /// Creates an instance of a Scorer with a given identifier,
        /// whose behavior is adjusted by a set of options
        /// </summary>
        /// <param name="identifier">Scorer identifier</param>
        /// <param name="options">Options</param>
        /// <returns></returns>
        public ScorerPlugin Emit(string identifier, Dictionary<string, string> options)
        {
            return (ScorerPlugin) base.Emit(identifier)
                .WithOptions(options);
        }
    }
}