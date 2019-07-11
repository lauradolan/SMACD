using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mono.Unix;
using SMACD.Shared.Attributes;
using SMACD.Shared.Data;
using SMACD.Shared.Extensions;
using SMACD.Shared.Plugins;
using SMACD.Shared.WorkspaceManagers;
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
        ///     If the Workspace has either been created or loaded
        /// </summary>
        public bool WorkspaceConfigured => !string.IsNullOrEmpty(WorkingDirectory);

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

        /// <summary>
        ///     Get the working directory for a given Plugin Pointer
        /// </summary>
        /// <param name="pointer">Plugin pointer</param>
        /// <returns></returns>
        public string GetChildWorkingDirectory(PluginPointerModel pointer)
        {
            // Configuration:
            // <storage root>/<workspace>/<resource>/<plugin>.<item hash>

            if (string.IsNullOrEmpty(WorkingDirectory))
                throw new Exception(
                    "Attempted to create child in ephemeral workspace; this operation is not allowed without a persistent working directory");

            var _workingDirectory = "";
            if (pointer.Resource == null)
                _workingDirectory = Path.Combine(WorkingDirectory, "no_resource_given",
                    $"{pointer.Plugin}.{pointer.Fingerprint(serializeEphemeralData: true)}");
            else
                _workingDirectory = Path.Combine(WorkingDirectory, pointer.Resource.ResourceId,
                    $"{pointer.Plugin}.{pointer.Fingerprint(serializeEphemeralData: true)}");

            if (!Directory.Exists(_workingDirectory))
            {
                Directory.CreateDirectory(_workingDirectory);
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    new UnixFileInfo(_workingDirectory).FileAccessPermissions = FileAccessPermissions.AllPermissions;
            }

            return _workingDirectory;
        }

        /// <summary>
        ///     Scan all items on the loaded map
        /// </summary>
        /// <returns></returns>
        public async Task<VulnerabilitySummary> ScanEntireMap()
        {
            Logger.LogInformation("Beginning to scan loaded map");
            var resultTasks = new List<Task<PluginResult>>();
            IteratePluginPointers((feature, useCase, abuseCase, pluginPointer) =>
                resultTasks.Add(Execute(pluginPointer)));

            while (!TaskManager.Instance.IsEmpty) Thread.Sleep(100);
            Logger.LogInformation("Completed executing {0} plugins from given map", resultTasks.Count);

            Logger.LogInformation("Artifacts processed successfully, beginning summarization");
            return await SummarizeArtifacts(await Task.WhenAll(resultTasks));
        }

        /// <summary>
        ///     Reprocess all items on the loaded map. Reprocessing involves reparsing the scanner-generated artifacts and
        ///     regenerating the summary report
        /// </summary>
        /// <returns></returns>
        public async Task<VulnerabilitySummary> ReprocessEntireMap()
        {
            Logger.LogInformation("Beginning to reprocess data from working directory {0}", WorkingDirectory);
            var resultTasks = new List<Task<PluginResult>>();
            IteratePluginPointers((feature, useCase, abuseCase, pluginPointer) =>
                resultTasks.Add(Reprocess(pluginPointer)));
            var resultInstances = await Task.WhenAll(resultTasks);

            return await SummarizeArtifacts(await Task.WhenAll(resultTasks));
        }

        /// <summary>
        ///     Retrieve a list of extensions currently loaded
        /// </summary>
        /// <returns></returns>
        public static IList<Tuple<string, string>> GetLoadedExtensions()
        {
            var loaded = new List<Tuple<string, string>>();
            loaded.AddRange(PluginManager.Instance.LoadedLibraryTypes.Select(t =>
            {
                var attr = t.GetConfigAttribute<PluginMetadataAttribute, PluginMetadataAttribute>(a => a);
                return attr == null ? null : Tuple.Create(attr.Identifier, attr.Name);
            }));

            loaded.AddRange(ResourceManager.GetKnownResourceHandlers()
                .Select(h => Tuple.Create(h.Item1, $"{h.Item1} Resource Handler")));

            loaded.AddRange(ServiceHookManager.Instance.LoadedLibraryTypes.Select(t =>
            {
                var attr = t.GetConfigAttribute<ServiceHookMetadataAttribute, ServiceHookMetadataAttribute>(a => a);
                return attr == null ? null : Tuple.Create(attr.Identifier, attr.Name);
            }));

            loaded.RemoveAll(i => i == null);
            return loaded;
        }

        /// <summary>
        ///     Validate a Plugin Pointer to ensure its target is acceptable
        /// </summary>
        /// <param name="pointer">Plugin Pointer to validate</param>
        /// <returns></returns>
        public bool Validate(PluginPointerModel pointer)
        {
            var instance = PluginManager.Instance.GetInstance(pointer);
            if (instance == null) return false;
            return instance.Validate(pointer);
        }

        private async Task<VulnerabilitySummary> SummarizeArtifacts(IList<PluginResult> results)
        {
            var summary = new VulnerabilitySummary {ResultInstances = results};

            // Run single-execution tasks for each of the result objects
            Logger.LogInformation("Executing single-run tasks for {0} result objects", results.Count);
            await Task.WhenAll(results.Select(r => r.SummaryRunOnce(summary)));
            Logger.LogInformation("Completed single-run execution against {0} result objects", results.Count);

            // Run generations of multi-execution tasks
            var changesOccurredThisGeneration = true;
            var sw = new Stopwatch();
            sw.Start();
            var generations = 0;
            while (changesOccurredThisGeneration)
            {
                generations++;
                foreach (var g in results)
                    changesOccurredThisGeneration = await g.SummaryRunGenerationally(summary);
                Logger.LogDebug("Completed summary generation {0} ... Converged? {1}", generations,
                    !changesOccurredThisGeneration);
            }

            sw.Stop();
            Logger.LogInformation("Completed summary report in {0} over {1} generations", sw.Elapsed, generations);
            return summary;
        }

        /// <summary>
        ///     Iterate over all PluginPointerModels in the Service Map, passing in all of each of their parents
        /// </summary>
        /// <param name="action">Action to take</param>
        public void IteratePluginPointers(Action<FeatureModel, UseCaseModel, AbuseCaseModel, PluginPointerModel> action)
        {
            foreach (var feature in Features)
            foreach (var useCase in feature.UseCases)
            foreach (var abuseCase in useCase.AbuseCases)
            foreach (var pluginPtr in abuseCase.PluginPointers)
                action(feature, useCase, abuseCase, pluginPtr);
        }

        /// <summary>
        ///     Execute a single Plugin from its Plugin Pointer
        /// </summary>
        /// <param name="pointer">Plugin pointer</param>
        /// <returns></returns>
        public Task<PluginResult> Execute(PluginPointerModel pointer)
        {
            Logger.LogInformation("Resolving plugin pointer id {0}", pointer.Plugin);
            var plugin = PluginManager.Instance.GetInstance(pointer);
            if (plugin == null)
                throw new Exception($"Plugin '{pointer.Plugin}' is not loaded");

            if (pointer.Resource != null)
            {
                Logger.LogInformation("Resolving resource pointer id {0}", pointer.Resource.ResourceId);
                if (!ResourceManager.Instance.ContainsPointer(pointer.Resource))
                    throw new Exception($"Resource '{pointer.Resource.ResourceId}' does not exist in resource map");
            }

            return TaskManager.Instance.Enqueue(plugin.GetValidatedExecutionTask(pointer));
        }

        /// <summary>
        ///     Reprocess a single Plugin Pointer
        /// </summary>
        /// <param name="pointer">Plugin pointer</param>
        /// <returns></returns>
        public Task<PluginResult> Reprocess(PluginPointerModel pointer)
        {
            Logger.LogInformation("Resolving plugin pointer id {0}", pointer.Plugin);
            var plugin = PluginManager.Instance.GetInstance(pointer);
            if (plugin == null)
                throw new Exception($"Plugin '{pointer.Plugin}' is not loaded");

            return TaskManager.Instance.Enqueue(plugin.Reprocess(GetChildWorkingDirectory(pointer)));
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