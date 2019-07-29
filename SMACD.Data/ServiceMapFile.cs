using SMACD.Data.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SMACD.Data
{
    /// <summary>
    ///     Service Map - Stores both Features and Resources for an application's test
    /// </summary>
    public class ServiceMapFile : IModel
    {
        /// <summary>
        ///     When this file was created originally
        /// </summary>
        public DateTime Created { get; set; }

        /// <summary>
        ///     When this file was last updated
        /// </summary>
        public DateTime Updated { get; set; }

        /// <summary>
        ///     Features stored in this Project Map
        /// </summary>
        public IList<FeatureModel> Features { get; set; } = new List<FeatureModel>();

        /// <summary>
        ///     Resources stored in this Service Map
        /// </summary>
        public IList<ResourceModel> Resources { get; set; } = new List<ResourceModel>();

        /// <summary>
        ///     Deserialize a Service Map from a given file
        /// </summary>
        /// <param name="file">File containing Service Map</param>
        /// <returns></returns>
        public static ServiceMapFile GetServiceMap(string file)
        {
            ServiceMapFile serviceMap;
            using (var sr = new StreamReader(file))
            {
                serviceMap = new DeserializerBuilder()
                    .WithNamingConvention(new CamelCaseNamingConvention())
                    .AddLoadedTagMappings()
                    .Build()
                    .Deserialize<ServiceMapFile>(sr);
            }

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

    internal static class ServiceMapFileExtensions
    {
        internal static T AddLoadedTagMappings<T>(this T builder)
            where T : BuilderSkeleton<T>
        {
            var types = new Dictionary<string, Type>
            {
                {"!http", typeof(HttpResourceModel)},
                {"!socketport", typeof(SocketPortResourceModel)}
            };
            foreach (var kvp in types)
                builder = builder.WithTagMapping(kvp.Key, kvp.Value);
            return builder;
        }
    }
}