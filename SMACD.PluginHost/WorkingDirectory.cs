using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using SMACD.PluginHost.Extensions;
using SMACD.PluginHost.Plugins;

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
        protected WorkingDirectory(string directory)
        {
            
            if (!directory.EndsWith(Path.DirectorySeparatorChar)) directory += Path.DirectorySeparatorChar;
            Location = Path.GetFullPath(Path.GetDirectoryName(directory));
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            Global.LogFactory.CreateLogger("WorkingDirectory").LogDebug("{0} created around {1}", GetType().Name, Location);
        }

        /// <summary>
        /// Base location where all Working Directories will be stored. (Default is %TEMP%\SMACD\*)
        /// </summary>
        public static string WorkingDirectoryBaseLocation { get; set; } = Path.Combine(Path.GetTempPath(), "SMACD", RandomExtensions.RandomName());

        /// <summary>
        /// Location of Working Directory
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Configuration of the Working Directory for this module
        /// </summary>
        public WorkingDirectoryLayout Configuration { get; protected set; }

        /// <summary>
        /// Return the path of a file relative to the Working Directory
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <returns></returns>
        public string WithFile(string fileName) => Path.Combine(Location, fileName);
        
        /// <summary>
        ///     Compute the Working Directory for a scenario where only a Resource ID is known
        /// </summary>
        /// <param name="resourceId">Resource ID</param>
        /// <returns></returns>
        public static string ComputeWorkingDirectory(string resourceId)
        {
            return Path.Combine(WorkingDirectoryBaseLocation,
                !string.IsNullOrEmpty(resourceId) ? resourceId : "_NO_RESOURCE_");
        }

        /// <summary>
        ///     Compute the Working Directory for a scenario where only a Plugin ID and its Options hashcode are known
        /// </summary>
        /// <param name="pluginId">Plugin identifier</param>
        /// <param name="hashCode">Hashcode of Plugin's Options object</param>
        /// <returns></returns>
        public static string ComputeWorkingDirectory(string pluginId, string hashCode)
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
        ///     Compute the Working Directory for a scenario where both the Resource ID and Plugin ID/Options are known
        /// </summary>
        /// <param name="resourceId">Resource ID</param>
        /// <param name="pluginId">Plugin ID</param>
        /// <param name="hashCode">Plugin Options Hashcode</param>
        /// <returns></returns>
        public static string ComputeWorkingDirectory(string resourceId, string pluginId, string hashCode)
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
        /// Commit the Working Directory configuration to disk
        /// </summary>
        public abstract void Commit();

        /// <summary>
        ///     Load a serialized object
        /// </summary>
        /// <param name="fileName">File name (no path)</param>
        public T LoadResultArtifact<T>(string fileName)
        {
            return (T) Global.DeserializeFromFile(WithFile(fileName), typeof(T));
        }

        public string ResourceId => Location.Substring(WorkingDirectoryBaseLocation.Length + 1).Split(Path.DirectorySeparatorChar)[0];

        public string PluginId => Location.Substring(WorkingDirectoryBaseLocation.Length + 1).Split(Path.DirectorySeparatorChar)[1];

        public string PluginOptionsHashCode => Location.Substring(WorkingDirectoryBaseLocation.Length + 1).Split(Path.DirectorySeparatorChar)[2];

        public override string ToString() =>  $"{Location} - {Configuration.Options.Count} generations deep";
    }

    public class ResourceWorkingDirectory : WorkingDirectory
    {
        private const string RESOURCE_WORKING_DIRECTORY_CONFIG = ".r_config";

        /// <summary>
        /// Represents a Working Directory that contains items pertaining to a specific resource
        /// </summary>
        /// <param name="directory">Directory to wrap with the WorkingDirectory</param>
        public ResourceWorkingDirectory(string directory) : base(directory)
        {
            if (File.Exists(WithFile(RESOURCE_WORKING_DIRECTORY_CONFIG)))
            {
                Configuration = (ResourceWorkingDirectoryLayout) Global.DeserializeFromFile(
                    WithFile(RESOURCE_WORKING_DIRECTORY_CONFIG), typeof(ResourceWorkingDirectoryLayout));
            }
            else
            {
                Configuration = new ResourceWorkingDirectoryLayout();
                Global.SerializeToFile(Configuration, new string[] { }, WithFile(RESOURCE_WORKING_DIRECTORY_CONFIG));
            }
        }

        /// <summary>
        /// The configuration of the Resource Working Directory, which contains the chain of Plugins run on the resource
        /// </summary>
        public new ResourceWorkingDirectoryLayout Configuration
        {
            get => (ResourceWorkingDirectoryLayout) base.Configuration;
            set => base.Configuration = value;
        }

        /// <summary>
        /// Retrieve the most recently executed module on this Resource based on PluginType
        /// </summary>
        /// <param name="type">Type to retrieve</param>
        /// <returns></returns>
        public WorkingDirectory GetMostRecent(PluginTypes type)
        {
            var target = Configuration.GetLast(type);
            if (target == null) return null;

            return new PluginWorkingDirectory(WorkingDirectory.ComputeWorkingDirectory(
                target.ResourceIds.First(),
                target.Identifier,
                target.Options.Fingerprint()));
        }

        /// <summary>
        /// Retrieve the most recently executed module on this Resource
        /// </summary>
        /// <returns></returns>
        public WorkingDirectory GetMostRecent()
        {
            var target = Configuration.GetLast();
            if (target == null) return null;

            return new PluginWorkingDirectory(WorkingDirectory.ComputeWorkingDirectory(
                target.ResourceIds.First(),
                target.Identifier,
                target.Options.Fingerprint()));
        }

        public override void Commit() => SaveResultArtifact(WithFile(RESOURCE_WORKING_DIRECTORY_CONFIG), Configuration);

        public override string ToString() => $"[Resource] " + base.ToString();
    }

    public class PluginWorkingDirectory : WorkingDirectory
    {
        private const string PLUGIN_WORKING_DIRECTORY_CONFIG = ".p_config";

        /// <summary>
        /// Represents a Working Directory that contains items pertaining to a specific plugin
        /// </summary>
        /// <param name="directory">Directory to wrap with the WorkingDirectory</param>
        public PluginWorkingDirectory(string directory) : base(directory)
        {
            if (File.Exists(WithFile(PLUGIN_WORKING_DIRECTORY_CONFIG)))
            {
                Configuration = (PluginWorkingDirectoryLayout) Global.DeserializeFromFile(
                    WithFile(PLUGIN_WORKING_DIRECTORY_CONFIG), typeof(PluginWorkingDirectoryLayout));
            }
            else
            {
                Configuration = new PluginWorkingDirectoryLayout();
                Global.SerializeToFile(Configuration, new string[] { }, WithFile(PLUGIN_WORKING_DIRECTORY_CONFIG));
            }

            ParentResource = new ResourceWorkingDirectory(Directory.GetParent(Location).Parent.FullName);
        }

        // TODO: Working Directories only support a single resource right now, despite other parts of the system supporting multiples
        // TODO: ... do we duplicate artifact content as a part of the WorkingDirectory automation?
        /// <summary>
        /// Retrieve the Working Directory associated with this Plugin's parent resource
        /// </summary>
        public ResourceWorkingDirectory ParentResource { get; set; }

        /// <summary>
        /// Retrieve the configuration for the Plugin's Working Directory
        /// </summary>
        public new PluginWorkingDirectoryLayout Configuration
        {
            get => (PluginWorkingDirectoryLayout) base.Configuration;
            set => base.Configuration = value;
        }

        public override void Commit() => SaveResultArtifact(WithFile(PLUGIN_WORKING_DIRECTORY_CONFIG), Configuration);

        public override string ToString() => $"[Plugin] " + base.ToString();
    }
}