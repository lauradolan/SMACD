using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Mono.Unix;
using SMACD.Shared.Data;
using SMACD.Shared.Extensions;
using SMACD.Shared.Resources;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SMACD.Shared.Plugins.AttackTools
{
    /// <summary>
    ///     Handles scanning and mapping of plugins
    /// </summary>
    public class AttackToolManager : LibraryManager<AttackTool>
    {
        private static readonly Lazy<AttackToolManager> _instance =
            new Lazy<AttackToolManager>(() => new AttackToolManager());

        private AttackToolManager() : base("SMACD.Plugins.*.dll", "AttackToolManager")
        {
        }

        public static AttackToolManager Instance => _instance.Value;

        /// <summary>
        ///     Scan all items on the loaded map
        /// </summary>
        /// <returns></returns>
        public Task ScanEntireMap()
        {
            Logger.LogInformation("Beginning to scan loaded map");
            var attackToolTasks = new List<Task>();
            foreach (var feature in Workspace.Instance.Features)
            foreach (var useCase in feature.UseCases)
            foreach (var abuseCase in useCase.AbuseCases)
            foreach (var pluginPointer in abuseCase.PluginPointers)
                attackToolTasks.Add(QueueAttackTool(pluginPointer));
            return Task.WhenAll(attackToolTasks);
        }

        /// <summary>
        ///     Execute a single Attack Tool from its Plugin Pointer
        /// </summary>
        /// <param name="pointer">Plugin pointer</param>
        /// <returns></returns>
        public Task QueueAttackTool(PluginPointerModel pointer)
        {
            Logger.LogInformation("Resolving plugin pointer id {0}", pointer.Plugin);
            var plugin = Instance.GetInstance(pointer.Plugin);
            if (plugin == null)
                throw new Exception($"Plugin '{pointer.Plugin}' is not loaded");

            if (pointer.Resource != null)
            {
                Logger.LogInformation("Resolving resource pointer id {0}", pointer.Resource.ResourceId);
                if (!ResourceManager.Instance.ContainsPointer(pointer.Resource))
                    throw new Exception($"Resource '{pointer.Resource.ResourceId}' does not exist in resource map");
            }

            return TaskManager.Instance.Enqueue(GetValidatedExecutionTask(pointer));
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

            if (string.IsNullOrEmpty(Workspace.Instance.WorkingDirectory))
                throw new Exception(
                    "Attempted to create child in ephemeral workspace; this operation is not allowed without a persistent working directory");

            var workingDirectory = "";
            if (pointer.Resource == null)
                workingDirectory = Path.Combine(Workspace.Instance.WorkingDirectory, "no_resource_given",
                    $"{pointer.Plugin}.{pointer.Fingerprint(serializeEphemeralData: true)}");
            else
                workingDirectory = Path.Combine(Workspace.Instance.WorkingDirectory, pointer.Resource.ResourceId,
                    $"{pointer.Plugin}.{pointer.Fingerprint(serializeEphemeralData: true)}");

            if (!Directory.Exists(workingDirectory))
            {
                Directory.CreateDirectory(workingDirectory);
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    new UnixFileInfo(workingDirectory).FileAccessPermissions = FileAccessPermissions.AllPermissions;
            }

            if (!File.Exists(Path.Combine(workingDirectory, ".ptr")))
                using (var sw = new StreamWriter(Path.Combine(workingDirectory, ".ptr")))
                {
                    var str = new SerializerBuilder()
                        .WithNamingConvention(new CamelCaseNamingConvention())
                        .Build()
                        .Serialize(pointer);
                    sw.WriteLine(str);
                }

            return workingDirectory;
        }

        /// <summary>
        ///     Retrieve a Task that will execute this Plugin (inside a wrapper)
        ///     This Task will be tagged with its Plugin name
        /// </summary>
        /// <param name="pointer">Pointer that describes the Plugin and its options</param>
        /// <returns></returns>
        private Task GetValidatedExecutionTask(PluginPointerModel pointer)
        {
            AttackTool plugin;
            try
            {
                plugin = (AttackTool)pointer.Validate();
            }
            catch (Exception ex)
            {
                Logger.LogCritical(ex, "Error validating Plugin Pointer for execution");
                return Task.FromResult(0);
            }

            return new Task(() =>
            {
                try
                {
                    Logger.LogDebug("Starting scheduled task -- Plugin: {0} -- Resource: {1}", pointer.Plugin,
                        pointer.Resource?.ResourceId);
                    plugin.Execute(pointer, AttackToolManager.Instance.GetChildWorkingDirectory(pointer)).Wait();
                    Logger.LogDebug("Completed scheduled task -- Plugin: {0} -- Resource: {1}", pointer.Plugin,
                        pointer.Resource?.ResourceId);
                }
                catch (Exception ex)
                {
                    Logger.LogCritical(ex, "Exception thrown while executing scheduled task");
                }
            });
        }
    }
}