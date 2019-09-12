using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using SMACD.Artifacts;
using Synthesys.SDK.Attributes;
using Synthesys.SDK.Extensions;

namespace Synthesys
{
    /// <summary>
    ///     Configure Extensions
    /// </summary>
    public static class ExtensionConfigurator
    {
        private static ILogger Logger { get; } = Global.LogFactory.CreateLogger("ExtensionConfigurator");

        /// <summary>
        ///     Configure an ActionExtension
        /// </summary>
        /// <param name="extension">ActionExtension to configure</param>
        /// <param name="artifactRoot">Artifact representing target</param>
        /// <param name="options">ActionExtension options</param>
        /// <returns></returns>
        public static ActionExtension Configure(
            this ActionExtension extension,
            Artifact artifactRoot,
            Dictionary<string, string> options)
        {
            extension.SetLoggerName(extension.GetType().Name);
            ConfigureExtensionOptions(extension, options);
            ConfigureArtifactProperty(extension, artifactRoot);
            return extension;
        }

        /// <summary>
        ///     Configure a ReactionExtension
        /// </summary>
        /// <param name="extension">ReactionExtension to configure</param>
        /// <returns></returns>
        public static ReactionExtension Configure(
            this ReactionExtension extension)
        {
            return extension;
        }

        private static Extension ConfigureExtensionOptions(Extension extensionInstance,
            Dictionary<string, string> options)
        {
            var actionInstancePropertyTypes = extensionInstance.GetType().GetProperties();

            foreach (var pair in options)
            {
                // Look for the [Configurable] attribute on the property and set the property from the options dictionary if it was found
                var propertyIsConfigurable =
                    actionInstancePropertyTypes.FirstOrDefault(t =>
                        t.Name.Equals(pair.Key, StringComparison.OrdinalIgnoreCase) &&
                        t.GetCustomAttribute<ConfigurableAttribute>() != null);
                if (propertyIsConfigurable != null)
                {
                    Logger.LogInformation("Found Configurable Property {0} (Type: {1}) -- Setting value {2}",
                        propertyIsConfigurable.Name, propertyIsConfigurable.PropertyType.Name, pair.Value);
                    propertyIsConfigurable.SetValue(extensionInstance,
                        Convert.ChangeType(pair.Value, propertyIsConfigurable.PropertyType));
                }
                else
                {
                    Logger.LogWarning("Specified option '{0}' which is not understood by plugin '{1}'", pair.Key,
                        extensionInstance.GetType().GetCustomAttribute<ExtensionAttribute>().ExtensionIdentifier);
                }
            }

            return extensionInstance;
        }

        private static Extension ConfigureArtifactProperty(Extension extensionInstance, Artifact resourceArtifact)
        {
            var artifactProperties = extensionInstance.GetType().GetProperties().Where(p =>
                typeof(Artifact).IsAssignableFrom(p.PropertyType)).ToArray();

            if (!artifactProperties.Any())
            {
                Logger.LogWarning("Extension does not have Artifact-derived property");
                return extensionInstance;
            }

            foreach (var artifactProperty in artifactProperties)
            {
                Artifact value = null;
                value = resourceArtifact.GetPathToRoot()
                    .FirstOrDefault(a => a.GetType() == artifactProperty.PropertyType);
                if (value != null) artifactProperty.SetValue(extensionInstance, value);
            }

            return extensionInstance;
        }
    }
}