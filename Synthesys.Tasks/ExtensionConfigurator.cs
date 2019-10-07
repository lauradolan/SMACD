using Microsoft.Extensions.Logging;
using SMACD.Artifacts;
using Synthesys.SDK.Attributes;
using Synthesys.SDK.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Synthesys.Tasks
{
    /// <summary>
    ///     Configure Extensions
    /// </summary>
    public static class ExtensionConfigurator
    {
        private static ILogger Logger { get; } = Global.LogFactory.CreateLogger("ExtensionConfigurator");

        /// <summary>
        ///     Configure an Extension
        /// </summary>
        /// <param name="extension">Extension to configure</param>
        /// <param name="artifactRoot">Artifact representing target</param>
        /// <param name="options">Extension options</param>
        /// <returns></returns>
        public static Extension Configure(
            this Extension extension,
            Artifact artifactRoot,
            Dictionary<string, string> options)
        {
            extension.SetLoggerName(extension.GetType().Name);
            ApplyConfigurableOptions(extension, options);
            ApplyArtifactProperty(extension, artifactRoot);

            ExtensionAttribute metadata = extension.GetType().GetCustomAttribute<ExtensionAttribute>();

            if (!extension.ValidateEnvironmentReadiness())
            {
                Logger.LogCritical("Environment readiness checks failed for Extension type {0}", metadata.ExtensionIdentifier);
                return null;
            }

            if (extension is ReactionExtension)
            {
                Logger.LogInformation("Running Initialize routine on Reaction Extension {0}", metadata.ExtensionIdentifier);
                ((ReactionExtension)extension).Initialize();
                Logger.LogInformation("Completed Initialize routine on Reaction Extension {0}", metadata.ExtensionIdentifier);
            }

            return extension;
        }

        /// <summary>
        ///     Applies matching values to properties marked as [Configurable]
        /// </summary>
        /// <param name="extensionInstance">Extension to configure</param>
        /// <param name="options">Options to apply</param>
        /// <returns></returns>
        private static Extension ApplyConfigurableOptions(Extension extensionInstance,
            Dictionary<string, string> options)
        {
            PropertyInfo[] actionInstancePropertyTypes = extensionInstance.GetType().GetProperties();

            foreach (KeyValuePair<string, string> pair in options)
            {
                // Look for the [Configurable] attribute on the property and set the property from the options dictionary if it was found
                PropertyInfo propertyIsConfigurable =
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

        /// <summary>
        ///     Applies Artifact anchor to the appropriate Type-matched property in the Extension
        /// </summary>
        /// <param name="extensionInstance">Extension to configure</param>
        /// <param name="resourceArtifact">Artifact to connect</param>
        /// <returns></returns>
        private static Extension ApplyArtifactProperty(Extension extensionInstance, Artifact resourceArtifact)
        {
            PropertyInfo[] artifactProperties = extensionInstance.GetType().GetProperties().Where(p =>
                typeof(Artifact).IsAssignableFrom(p.PropertyType)).ToArray();

            if (!artifactProperties.Any())
            {
                Logger.LogWarning("Extension does not have Artifact-derived property");
                return extensionInstance;
            }

            foreach (PropertyInfo artifactProperty in artifactProperties)
            {
                Artifact value = null;
                value = resourceArtifact.GetNodesToRoot()
                    .FirstOrDefault(a => a.GetType() == artifactProperty.PropertyType);
                if (value != null)
                {
                    artifactProperty.SetValue(extensionInstance, value);
                }
            }

            return extensionInstance;
        }
    }
}