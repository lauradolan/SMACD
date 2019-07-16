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

        public ServicePlugin Emit(string identifier, Dictionary<string, string> options)
        {
            return (ServicePlugin) base.Emit(identifier)
                .WithOptions(options);
        }
    }
}