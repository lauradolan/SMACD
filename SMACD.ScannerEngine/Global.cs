using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using SMACD.Data;
using SMACD.ScannerEngine.Attributes;
using SMACD.ScannerEngine.Extensions;
using SMACD.ScannerEngine.Factories;
using SMACD.ScannerEngine.Resources;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SMACD.ScannerEngine
{
    public static class Global
    {
        public static ILoggerFactory LogFactory { get; } = new LoggerFactory();

        private static ILogger Logger { get; } = LogFactory.CreateLogger("Global");

        /// <summary>
        ///     Retrieve a list of extensions currently loaded
        /// </summary>
        /// <returns></returns>
        public static IList<Tuple<string, string>> GetLoadedExtensions()
        {
            var loaded = new List<Tuple<string, string>>();
            loaded.AddRange(
                AttackToolPluginFactory.Instance.LoadedPluginTypes
                    .Union(ServicePluginFactory.Instance.LoadedPluginTypes)
                    .Select(t =>
                    {
                        var attr = PluginMetadataAttribute.Get(t.Value);
                        return attr == null ? null : Tuple.Create(attr.Identifier, attr.Name);
                    }));

            loaded.AddRange(ResourceManager.GetKnownResourceHandlers()
                .Select(h => Tuple.Create(h.Item1, $"{h.Item1} Resource Handler")));

            loaded.RemoveAll(i => i == null);
            return loaded;
        }

        /// <summary>
        ///     Deserialize a Service Map from a given file
        /// </summary>
        /// <param name="file">File containing Service Map</param>
        /// <returns></returns>
        public static ServiceMapFile GetServiceMap(string file)
        {
            Logger.LogDebug("Reading Service Map {0}", file);

            ServiceMapFile serviceMap;
            using (var sr = new StreamReader(file))
            {
                serviceMap = new DeserializerBuilder()
                    .WithNamingConvention(new CamelCaseNamingConvention())
                    .AddLoadedTagMappings()
                    .Build()
                    .Deserialize<ServiceMapFile>(sr);
            }

            Logger.LogInformation("Read {0} Features and {1} Resources from Service Map", serviceMap.Features.Count,
                serviceMap.Resources.Count);

            Logger.LogDebug("Registering {1} Resources in ResourceManager", serviceMap.Resources.Count);
            foreach (var resource in serviceMap.Resources)
                if (ResourceManager.Instance.Register(resource) == null)
                    Logger.LogWarning("Could not successfully register resource {0}", resource.ResourceId);

            return serviceMap;
        }

        /// <summary>
        ///     Serialize a Service Map to a given file
        /// </summary>
        /// <param name="serviceMap">Service Map to serialize</param>
        /// <param name="file">File to serialize Service Map to</param>
        /// <returns></returns>
        public static void PutServiceMap(ServiceMapFile serviceMap, string file)
        {
            // Change the metadata to reflect this updated version
            serviceMap.Updated = DateTime.Now;

            using (var sr = new StreamWriter(file))
            {
                new SerializerBuilder()
                    .WithNamingConvention(new CamelCaseNamingConvention())
                    .AddLoadedTagMappings()
                    .Build()
                    .Serialize(sr, serviceMap);
            }
        }
    }
}