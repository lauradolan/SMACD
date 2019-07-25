using SMACD.PluginHost.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SMACD.PluginHost
{
    // Configuration:
    // <storage root>
    // `- Workspace ID
    //    |- Resource ID
    //    `- Resource ID
    //       |- .r_config
    //       |  |- Plugin run order
    //       |  `- Resource configuration
    //       |- Plugin ID
    //       `- Plugin ID
    //          |- .p_config
    //          |  `- Plugin Options
    //          `- Plugin artifacts

    public abstract class WorkingDirectory
    {
        public static string WorkingDirectoryBaseLocation { get; set; } = Path.Combine(Path.GetTempPath(), "SMACD");

        public string Location { get; set; }
        public string WithFile(string fileName) => Path.Combine(Location, fileName);
        public WorkingDirectoryLayout Configuration { get; protected set; }

        public static string ComputeWorkingDirectory(Plugin plugin) => ComputeWorkingDirectory(
            plugin.Resources.First().ResourceId,
            plugin.PluginDescription.Identifier,
            plugin.Options.GetHashCode());

        /// <summary>
        /// Compute the Working Directory for a scenario where only a Resource ID is known
        /// </summary>
        /// <param name="resourceId">Resource ID</param>
        /// <returns></returns>
        public static string ComputeWorkingDirectory(string resourceId)
        {
            return Path.Combine(WorkingDirectoryBaseLocation, !string.IsNullOrEmpty(resourceId) ? resourceId : "_NO_RESOURCE_");
        }

        /// <summary>
        /// Compute the Working Directory for a scenario where only a Plugin ID and its Options hashcode are known
        /// </summary>
        /// <param name="pluginId">Plugin identifier</param>
        /// <param name="hashCode">Hashcode of Plugin's Options object</param>
        /// <returns></returns>
        public static string ComputeWorkingDirectory(string pluginId, int hashCode)
        {
            return
                WorkingDirectoryBaseLocation +
                Path.DirectorySeparatorChar +
                "_NO_RESOURCE_" +
                Path.DirectorySeparatorChar +
                pluginId +
                Path.DirectorySeparatorChar +
                hashCode;
        }

        /// <summary>
        /// Compute the Working Directory for a scenario where both the Resource ID and Plugin ID/Options are known
        /// </summary>
        /// <param name="resourceId">Resource ID</param>
        /// <param name="pluginId">Plugin ID</param>
        /// <param name="hashCode">Plugin Options Hashcode</param>
        /// <returns></returns>
        public static string ComputeWorkingDirectory(string resourceId, string pluginId, int hashCode)
        {
            if (!string.IsNullOrEmpty(resourceId))
                return
                    WorkingDirectoryBaseLocation +
                    Path.DirectorySeparatorChar +
                    resourceId +
                    Path.DirectorySeparatorChar +
                    pluginId +
                    Path.DirectorySeparatorChar +
                    hashCode;
            return
                WorkingDirectoryBaseLocation +
                Path.DirectorySeparatorChar +
                "_NO_RESOURCE_" +
                Path.DirectorySeparatorChar +
                pluginId +
                Path.DirectorySeparatorChar +
                hashCode;
        }

        protected WorkingDirectory(string directory)
        {
            Location = directory;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
        }

        /// <summary>
        ///     Save a serialized object
        /// </summary>
        /// <param name="fileName">File name (no path)</param>
        /// <param name="obj">Object to save</param>
        public void SaveResultArtifact<T>(string fileName, T obj)
        {
            Global.SerializeToFile(obj, Array.Empty<string>(), WithFile(fileName));
        }

        /// <summary>
        ///     Load a serialized object
        /// </summary>
        /// <param name="fileName">File name (no path)</param>
        public T LoadResultArtifact<T>(string fileName)
        {
            return (T)Global.DeserializeFromFile(WithFile(fileName), typeof(T));
        }
    }

    public class ResourceWorkingDirectory : WorkingDirectory
    {
        private const string RESOURCE_WORKING_DIRECTORY_CONFIG = ".r_config";

        public new ResourceWorkingDirectoryLayout Configuration
        {
            get => (ResourceWorkingDirectoryLayout)base.Configuration;
            set => base.Configuration = value;
        }

        public List<PluginSummary> PluginChain { get; set; } = new List<PluginSummary>();

        public ResourceWorkingDirectory(string directory) : base(directory)
        {
            if (File.Exists(WithFile(RESOURCE_WORKING_DIRECTORY_CONFIG)))
                Configuration = (ResourceWorkingDirectoryLayout)Global.DeserializeFromFile(
                    WithFile(RESOURCE_WORKING_DIRECTORY_CONFIG), typeof(ResourceWorkingDirectoryLayout));
            else
            {
                Configuration = new ResourceWorkingDirectoryLayout();
                Global.SerializeToFile(Configuration, new string[] { }, RESOURCE_WORKING_DIRECTORY_CONFIG);
            }
        }
    }

    public class PluginWorkingDirectory : WorkingDirectory
    {
        private const string PLUGIN_WORKING_DIRECTORY_CONFIG = ".p_config";

        public ResourceWorkingDirectory ParentResource => new ResourceWorkingDirectory(Path.Combine(base.Location, ".."));

        public new PluginWorkingDirectoryLayout Configuration
        {
            get => (PluginWorkingDirectoryLayout)base.Configuration;
            set => base.Configuration = value;
        }

        public PluginWorkingDirectory(string directory) : base(directory)
        {
            if (File.Exists(WithFile(PLUGIN_WORKING_DIRECTORY_CONFIG)))
                Configuration = (PluginWorkingDirectoryLayout)Global.DeserializeFromFile(
                    WithFile(PLUGIN_WORKING_DIRECTORY_CONFIG), typeof(PluginWorkingDirectoryLayout));
            else
            {
                Configuration = new PluginWorkingDirectoryLayout();
                Global.SerializeToFile(Configuration, new string[] { }, PLUGIN_WORKING_DIRECTORY_CONFIG);
            }
        }
    }
}
