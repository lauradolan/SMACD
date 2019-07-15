using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using SMACD.Shared.Attributes;
using SMACD.Shared.Data;
using SMACD.Shared.Extensions;
using SMACD.Shared.Plugins.AttackTools;
using SMACD.Shared.Plugins.Services;
using SMACD.Shared.Resources;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SMACD.Shared
{
    /// <summary>
    ///     Represents a base working directory (to contain plugin working directories) and its Service Map
    ///     assets; Features and Resources. Also contains a TaskManager to handle multitasking.
    /// </summary>
    public class Workspace
    {
        private const string WORKSPACE_SERVICE_MAP_MIRROR = "input.yaml";

        private static readonly Lazy<Workspace> _instance = new Lazy<Workspace>(() => new Workspace());

        private Workspace()
        {
        }

        public static string WORKSPACE_STORAGE { get; set; }
        public static Workspace Instance => _instance.Value;

        public static ILoggerFactory LogFactory { get; } = new LoggerFactory();

        /// <summary>
        ///     Features from the Service Map
        /// </summary>
        public IList<FeatureModel> Features => ServiceMap?.Features;

        /// <summary>
        ///     Working Directory to store output artifacts used by results
        /// </summary>
        public string WorkingDirectory { get; private set; }

        /// <summary>
        ///     Service Map currently loaded
        /// </summary>
        public ServiceMapFile ServiceMap { get; private set; }

        private ILogger Logger { get; } = LogFactory.CreateLogger(typeof(Workspace).Name);

        /// <summary>
        ///     Retrieve a list of extensions currently loaded
        /// </summary>
        /// <returns></returns>
        public static IList<Tuple<string, string>> GetLoadedExtensions()
        {
            var loaded = new List<Tuple<string, string>>();
            loaded.AddRange(AttackToolManager.Instance.LoadedLibraryTypes.Select(t =>
            {
                var attr = t.GetConfigAttribute<MetadataAttribute, MetadataAttribute>(a => a);
                return attr == null ? null : Tuple.Create(attr.Identifier, attr.Name);
            }));

            loaded.AddRange(ResourceManager.GetKnownResourceHandlers()
                .Select(h => Tuple.Create(h.Item1, $"{h.Item1} Resource Handler")));

            loaded.AddRange(ServiceHookManager.Instance.LoadedLibraryTypes.Select(t =>
            {
                var attr = t.GetConfigAttribute<MetadataAttribute, MetadataAttribute>(a => a);
                return attr == null ? null : Tuple.Create(attr.Identifier, attr.Name);
            }));

            loaded.RemoveAll(i => i == null);
            return loaded;
        }

        /// <summary>
        ///     Creates a Workspace without a persistent Working Directory (cannot create Plugin instances)
        /// </summary>
        /// <returns></returns>
        public void CreateEphemeral(string serviceMapFile = null)
        {
            if (string.IsNullOrEmpty(serviceMapFile))
            {
                Logger.LogDebug("Creating new ephemeral Workspace with blank Service Map");
                ServiceMap = new ServiceMapFile
                {
                    Created = DateTime.Now,
                    Updated = DateTime.Now
                };
            }
            else
            {
                Logger.LogDebug("Creating new ephemeral Workspace with Service Map from {0}", serviceMapFile);
                ImportServiceMapFromFile(serviceMapFile);
            }

            WorkingDirectory = null;
        }

        /// <summary>
        ///     Creates a new Workspace around a given Service Map
        /// </summary>
        /// <param name="serviceMapFile">Service Map file</param>
        /// <param name="workingDirectory">Working Directory, otherwise it is randomly generated</param>
        /// <returns></returns>
        public void Create(string serviceMapFile, string workingDirectory = null)
        {
            Logger.LogDebug("Creating new Workspace for Service Map {0}", serviceMapFile);
            if (!File.Exists(serviceMapFile))
            {
                Logger.LogCritical("Service Map {0} not found", serviceMapFile);
                return;
            }

            if (string.IsNullOrEmpty(workingDirectory))
            {
                var newId = RandomExtensions.RandomName();
                workingDirectory = Directory.GetCurrentDirectory();
                while (Directory.Exists(workingDirectory))
                    workingDirectory = Path.Combine(WORKSPACE_STORAGE, $"{newId.Replace(' ', '_')}");
                Directory.CreateDirectory(workingDirectory);
                Logger.LogInformation("Created Workspace '{0}' with working directory {1}", newId, workingDirectory);
            }
            else
            {
                Logger.LogInformation("Using existing directory {0}", workingDirectory);
            }

            try
            {
                File.Copy(serviceMapFile, Path.Combine(workingDirectory, WORKSPACE_SERVICE_MAP_MIRROR));
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Failed copying Service Map to working directory");
                return;
            }

            Logger.LogInformation("Copied Service Map from {0} to working directory {1}", serviceMapFile,
                Path.Combine(workingDirectory, WORKSPACE_SERVICE_MAP_MIRROR));

            ImportServiceMapFromFile(Path.Combine(workingDirectory, WORKSPACE_SERVICE_MAP_MIRROR));
            WorkingDirectory = workingDirectory;
        }

        /// <summary>
        ///     Load an existing Workspace from its Working Directory
        /// </summary>
        /// <param name="workingDirectory">Existing Workspace</param>
        public void Load(string workingDirectory)
        {
            Logger.LogInformation("Using existing Workspace from {0}", workingDirectory);
            if (!File.Exists(Path.Combine(workingDirectory, WORKSPACE_SERVICE_MAP_MIRROR)))
            {
                Logger.LogError("Workspace copy of Service Map not found in working directory");
                return;
            }

            ImportServiceMapFromFile(Path.Combine(workingDirectory, WORKSPACE_SERVICE_MAP_MIRROR));
            WorkingDirectory = workingDirectory;
        }

        /// <summary>
        ///     Deserialize a Service Map from a given file
        /// </summary>
        /// <param name="file">File containing Service Map</param>
        /// <returns></returns>
        public static ServiceMapFile GetServiceMap(string file)
        {
            using (var sr = new StreamReader(file))
            {
                return new DeserializerBuilder()
                    .WithNamingConvention(new CamelCaseNamingConvention())
                    .AddLoadedTagMappings()
                    .Build()
                    .Deserialize<ServiceMapFile>(sr);
            }
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

        private void ImportServiceMapFromFile(string serviceMapFile)
        {
            Logger.LogDebug("Reading Service Map {0}", serviceMapFile);
            ServiceMap = GetServiceMap(serviceMapFile);
            Logger.LogInformation("Read {0} Features and {1} Resources from Service Map", ServiceMap.Features.Count,
                ServiceMap.Resources.Count);

            Logger.LogDebug("Registering {1} Resources in ResourceManager", ServiceMap.Resources.Count);
            foreach (var resource in ServiceMap.Resources)
                if (ResourceManager.Instance.Register(resource) == null)
                    Logger.LogWarning("Could not successfully register resource {0}", resource.ResourceId);
                else
                    Logger.LogDebug("Registered {0} resource '{1}'", resource.GetType().Name, resource.ResourceId);
        }
    }
}