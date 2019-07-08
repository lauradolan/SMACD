using Microsoft.Extensions.Logging;
using SMACD.Shared.Attributes;
using SMACD.Shared.Data;
using SMACD.Shared.Plugins;
using SMACD.Shared.Resources;
using SMACD.Shared.WorkspaceManagers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SMACD.Shared
{
    /// <summary>
    /// Represents a base working directory (to contain plugin working directories) and its Service Map
    /// assets; Features and Resources. Also contains a TaskManager to handle multitasking.
    /// </summary>
    public class Workspace
    {
        public static string WORKSPACE_STORAGE { get; set; }
        private const string WORKSPACE_SERVICE_MAP_MIRROR = "input.yaml";

        private static readonly Lazy<Workspace> _instance = new Lazy<Workspace>(() => new Workspace());
        public static Workspace Instance => _instance.Value;

        /// <summary>
        /// Fired when the Workspace queue has completed
        /// </summary>
        public event EventHandler WorkspaceCompleted;

        /// <summary>
        /// Features from the Service Map
        /// </summary>
        public IList<FeatureModel> Features { get; set; }

        /// <summary>
        /// If the Workspace has either been created or loaded
        /// </summary>
        public bool WorkspaceConfigured => !string.IsNullOrEmpty(WorkingDirectory);

        /// <summary>
        /// Working Directory to store output artifacts used by results
        /// </summary>
        public string WorkingDirectory { get; private set; }

        private ILogger Logger { get; } = Extensions.LogFactory.CreateLogger(typeof(Workspace).Name);

        private Workspace()
        {
        }

        /// <summary>
        /// Deserialize a Service Map from a given file
        /// </summary>
        /// <param name="file">File containing Service Map</param>
        /// <returns></returns>
        public static ServiceMapFile GetServiceMap(string file)
        {
            using (var sr = new StreamReader(file))
            {
                var deserializer = new DeserializerBuilder();

                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    foreach (var type in asm.GetTypes())
                    {
                        var attr = type.GetConfigAttribute<ResourceIdentifierAttribute, string>(a => a.ResourceIdentifier);
                        if (attr != null)
                            deserializer = deserializer.WithTagMapping("!" + attr, type);
                    }

                return (deserializer
                    .WithNamingConvention(new CamelCaseNamingConvention())
                    .Build())
                    .Deserialize<ServiceMapFile>(sr);
            }
        }

        /// <summary>
        /// Get the working directory for a given Plugin Pointer
        /// </summary>
        /// <param name="pointer">Plugin pointer</param>
        /// <returns></returns>
        public string GetChildWorkingDirectory(PluginPointerModel pointer)
        {
            // Configuration:
            // <storage root>/<workspace>/<resource>/<plugin>.<item hash>

            string _workingDirectory = "";
            if (pointer.Resource == null)
                _workingDirectory = Path.Combine(WorkingDirectory, $"no_resource_given", $"{pointer.Plugin}.{pointer.Fingerprint(serializeEphemeralData: true)}");
            else
                _workingDirectory = Path.Combine(WorkingDirectory, pointer.Resource.ResourceId, $"{pointer.Plugin}.{pointer.Fingerprint(serializeEphemeralData: true)}");

            if (!Directory.Exists(_workingDirectory))
            {
                Directory.CreateDirectory(_workingDirectory);
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    (new Mono.Unix.UnixFileInfo(_workingDirectory)).FileAccessPermissions = Mono.Unix.FileAccessPermissions.AllPermissions;
                else
                    Logger.LogWarning("!!! Strange behavior *sometimes* occurs with some containers and Docker mounts between Linux and Windows !!!");
            }

            return _workingDirectory;
        }

        /// <summary>
        /// Scan all items on the loaded map
        /// </summary>
        /// <returns></returns>
        public async Task<VulnerabilitySummary> ScanEntireMap()
        {
            Logger.LogInformation("Beginning to scan loaded map");
            var resultTasks = new List<Task<PluginResult>>();
            IteratePluginPointers((feature, useCase, abuseCase, pluginPointer) => resultTasks.Add(Execute(pluginPointer)));

            while (!TaskManager.Instance.IsEmpty) System.Threading.Thread.Sleep(100);
            Logger.LogInformation("Completed executing {0} plugins from given map", resultTasks.Count);

            Logger.LogInformation("Artifacts processed successfully, beginning summarization");
            return await SummarizeArtifacts(await Task.WhenAll(resultTasks));
        }

        /// <summary>
        /// Reprocess all items on the loaded map. Reprocessing involves reparsing the scanner-generated artifacts and regenerating the summary report
        /// </summary>
        /// <returns></returns>
        public async Task<VulnerabilitySummary> ReprocessEntireMap()
        {
            Logger.LogInformation("Beginning to reprocess data from working directory {0}", WorkingDirectory);
            var resultTasks = new List<Task<PluginResult>>();
            IteratePluginPointers((feature, useCase, abuseCase, pluginPointer) => resultTasks.Add(Reprocess(pluginPointer)));
            var resultInstances = await Task.WhenAll(resultTasks);

            return await SummarizeArtifacts(await Task.WhenAll(resultTasks));
        }

        private async Task<VulnerabilitySummary> SummarizeArtifacts(IList<PluginResult> results)
        {
            var summary = new VulnerabilitySummary() { ResultInstances = results };

            // Run single-execution tasks for each of the result objects
            Logger.LogInformation("Executing single-run tasks for {0} result objects", results.Count);
            await Task.WhenAll(results.Select(r => r.SummaryRunOnce(summary)));
            Logger.LogInformation("Completed single-run execution against {0} result objects", results.Count);

            // Run generations of multi-execution tasks
            bool changesOccurredThisGeneration = true;
            var sw = new Stopwatch();
            sw.Start();
            var generations = 0;
            while (changesOccurredThisGeneration)
            {
                generations++;
                foreach (var g in results)
                    changesOccurredThisGeneration = await g.SummaryRunGenerationally(summary);
                Logger.LogDebug("Completed summary generation {0} ... Converged? {1}", generations, !changesOccurredThisGeneration);
            }
            sw.Stop();
            Logger.LogInformation("Completed summary report in {0} over {1} generations", sw.Elapsed, generations);
            return summary;
        }

        /// <summary>
        /// Iterate over all PluginPointerModels in the Service Map, passing in all of each of their parents
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
        /// Execute a single Plugin from its Plugin Pointer
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

            return TaskManager.Instance.Enqueue(plugin.GetValidatedExecutionTask(this, pointer));
        }

        /// <summary>
        /// Reprocess a single Plugin Pointer
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
                var newId = Extensions.RandomName();
                workingDirectory = Directory.GetCurrentDirectory();
                while (Directory.Exists(workingDirectory))
                    workingDirectory = Path.Combine(WORKSPACE_STORAGE, $"{newId.Replace(' ', '_')}");
                Directory.CreateDirectory(workingDirectory);
                Logger.LogInformation("Created Workspace '{0}' with working directory {1}", newId, workingDirectory);
            }
            else
                Logger.LogInformation("Using existing directory {0}", workingDirectory);

            try { File.Copy(serviceMapFile, Path.Combine(workingDirectory, WORKSPACE_SERVICE_MAP_MIRROR)); }
            catch (Exception ex) { Logger.LogCritical(ex, "Failed copying Service Map to working directory"); return; }
            Logger.LogInformation("Copied Service Map from {0} to working directory {1}", serviceMapFile, Path.Combine(workingDirectory, WORKSPACE_SERVICE_MAP_MIRROR));

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
            var map = GetServiceMap(serviceMapFile);
            Features = map.Features;
            Logger.LogInformation("Read {0} Features and {1} Resources from Service Map", map.Features.Count, map.Resources.Count);

            Logger.LogDebug("Registering {1} Resources in ResourceManager", map.Resources.Count);
            foreach (var resource in map.Resources)
            {
                if (ResourceManager.Instance.Register(resource) == null)
                    Logger.LogWarning("Could not successfully register resource {0}", resource.ResourceId);
                else
                    Logger.LogDebug("Registered {0} resource '{1}'", resource.GetType().Name, resource.ResourceId);
            }
        }
    }
}