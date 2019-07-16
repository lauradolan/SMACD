using System;
using System.Collections.Generic;
using SMACD.ScannerEngine.Plugins;

namespace SMACD.ScannerEngine.Factories
{
    public class ServicePluginFactory : PluginFactory<ServicePlugin>
    {
        private static readonly Lazy<ServicePluginFactory> _instance =
            new Lazy<ServicePluginFactory>(() => new ServicePluginFactory());

        public ServicePluginFactory() : base("SMACD.Services.*.dll")
        {
        }

        public static ServicePluginFactory Instance => _instance.Value;

        /// <summary>
        /// Creates an instance of a Service with a given identifier,
        /// whose behavior is adjusted by a set of options
        /// </summary>
        /// <param name="identifier">Service identifier</param>
        /// <param name="options">Options</param>
        /// <returns></returns>
        public ServicePlugin Emit(string identifier, Dictionary<string, string> options)
        {
            return (ServicePlugin) base.Emit(identifier)
                .WithOptions(options);
        }
    }
}