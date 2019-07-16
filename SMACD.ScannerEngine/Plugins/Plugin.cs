using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Mono.Unix;
using SMACD.Data;
using SMACD.ScannerEngine.Attributes;
using SMACD.ScannerEngine.Extensions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace SMACD.ScannerEngine.Plugins
{
    public abstract class Plugin
    {
        private string Identifier => PluginMetadataAttribute.Get(GetType()).Identifier;
        protected ILogger Logger { get; set; } = Global.LogFactory.CreateLogger("Plugin Host");

        public PluginPointerModel Pointer { get; set; } = new PluginPointerModel();

        public string WorkingDirectory { get; set; }

        internal virtual Plugin Validate()
        {
            var required = ConfigurableAttribute.GetProperties(GetType())
                .Where(p => p.GetCustomAttribute<RequiredAttribute>() != null).ToList();
            if (required.Any(r => r.GetValue(this) == null))
                throw new Exception("Required configuration options not provided: " +
                                    string.Join(", ", required.Select(r => r.Name).ToString()));
            return this;
        }

        /// <summary>
        ///     Get the working directory for a given Plugin Pointer
        /// </summary>
        /// <param name="workspaceWorkingDirectory">Working directory for Workspace</param>
        /// <param name="pointer">Plugin pointer</param>
        /// <returns></returns>
        public string GetChildWorkingDirectory(string workspaceWorkingDirectory, PluginPointerModel pointer)
        {
            // Configuration:
            // <storage root>/<workspace>/<resource>/<plugin>.<item hash>

            string workingDirectory;
            if (pointer.Resource == null)
                workingDirectory = Path.Combine(workspaceWorkingDirectory, "no_resource_given",
                    $"{pointer.Plugin}.{pointer.Fingerprint(serializeEphemeralData: true)}");
            else
                workingDirectory = Path.Combine(workspaceWorkingDirectory, pointer.Resource.ResourceId,
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
        ///     Project an options dictionary to properties in this object
        /// </summary>
        /// <param name="options">Options to project</param>
        internal Plugin WithOptions(IDictionary<string, string> options)
        {
            var propertyTargets = GetType().GetProperties();
            foreach (var pair in options)
            {
                var target =
                    propertyTargets.FirstOrDefault(t => t.Name.Equals(pair.Key, StringComparison.OrdinalIgnoreCase) &&
                                                        t.GetCustomAttribute<ConfigurableAttribute>() != null);
                if (target != null)
                    target.SetValue(this, Convert.ChangeType(pair.Value, target.PropertyType));
                else
                    Logger.LogWarning("Specified option '{0}' which is not understood by plugin '{1}'", pair.Key,
                        Identifier);
            }

            return this;
        }
    }
}