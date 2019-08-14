using Microsoft.Extensions.Logging;
using SMACD.Artifacts;
using SMACD.SDK.Attributes;
using SMACD.SDK.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SMACD.ScanEngine
{
    public static class ExtensionConfigurator
    {
        public static ActionExtension Configure(
            this ActionExtension extension,
            Artifact artifactRoot,
            Dictionary<string, string> options)
        {
            BindCoreFeatures(extension);
            ConfigureExtensionOptions(extension, options);
            ConfigureArtifactProperty(extension, artifactRoot);
            return extension;
        }

        public static ReactionExtension Configure(
            this ReactionExtension extension)
        {
            BindCoreFeatures(extension);
            return extension;
        }

        private static ILogger Logger { get; } = SMACD.ScanEngine.Global.LogFactory.CreateLogger("ExtensionConfigurator");

        private static Extension BindCoreFeatures(Extension extensionInstance)
        {
            MethodInfo coreBindMethod = typeof(Extension).GetMethod("BindCoreFeatures", BindingFlags.Instance | BindingFlags.NonPublic);
            coreBindMethod.Invoke(extensionInstance, new object[] { SMACD.ScanEngine.Global.LogFactory.CreateLogger(extensionInstance.GetType().Name) });
            return extensionInstance;
        }

        private static Extension ConfigureExtensionOptions(Extension extensionInstance, Dictionary<string, string> options)
        {
            PropertyInfo[] actionInstancePropertyTypes = extensionInstance.GetType().GetProperties();

            foreach (KeyValuePair<string, string> pair in options)
            {
                // Look for the [Configurable] attribute on the property and set the property from the options dictionary if it was found
                PropertyInfo propertyIsConfigurable =
                    actionInstancePropertyTypes.FirstOrDefault(t => t.Name.Equals(pair.Key, StringComparison.OrdinalIgnoreCase) &&
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
            PropertyInfo artifactProperty = extensionInstance.GetType().GetProperties().FirstOrDefault(p =>
                typeof(Artifact).IsAssignableFrom(p.PropertyType));

            if (artifactProperty == null)
            {
                Logger.LogWarning("Extension does not have Artifact-derived property");
                return extensionInstance;
            }

            Artifact value = null;
            value = resourceArtifact.GetPathToRoot().FirstOrDefault(a => a.GetType() == artifactProperty.PropertyType);
            if (value != null)
            {
                artifactProperty.SetValue(extensionInstance, value);
            }

            return extensionInstance;
        }
    }
}
