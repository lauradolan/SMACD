using Microsoft.Extensions.Logging;
using SMACD.Artifacts;
using SMACD.SDK;
using SMACD.SDK.Attributes;
using SMACD.SDK.Extensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SMACD.ScanEngine
{
    public class ExtensionToolbox
    {
        private static readonly Lazy<ExtensionToolbox> _instance = new Lazy<ExtensionToolbox>(() => new ExtensionToolbox());
        public static ExtensionToolbox Instance => _instance.Value;

        private Dictionary<string, Type> _actionExtensionMap =>
            _extensionLibraries.SelectMany(l => l.ActionExtensions)
            .ToDictionary(k => k.Key, v => v.Value);
        private Dictionary<TriggerDescriptor, List<Type>> _reactionExtensionMap =>
                    _extensionLibraries.SelectMany(l => l.ReactionExtensions)
                    .ToDictionary(k => k.Key, v => v.Value);

        private readonly List<ExtensionLibrary> _extensionLibraries = new List<ExtensionLibrary>();
        public IReadOnlyList<ExtensionLibrary> ExtensionLibraries => _extensionLibraries.AsReadOnly();

        protected ILogger Logger { get; } = Global.LogFactory.CreateLogger("TaskToolbox");

        public static Type ResolveType(string typeName)
        {
            return Instance.ExtensionLibraries.SelectMany(l => l.ProvidedTypes).FirstOrDefault(t => t.FullName == typeName);
        }

        public void LoadExtensionLibrariesFromPath(string path, string filePattern)
        {
            Directory.GetFiles(path, filePattern).ToList().ForEach(f => LoadExtensionLibrary(f));
        }

        public void LoadExtensionLibrary(string path)
        {
            _extensionLibraries.Add(new ExtensionLibrary(path));
        }

        public ActionExtension EmitAction(string actionIdentifier, Dictionary<string, string> options, Artifact artifactRoot)
        {
            ActionExtension instance = EmitConfiguredActionExtension(actionIdentifier, artifactRoot).Configure(artifactRoot, options);
            if (!instance.ValidateEnvironmentReadiness())
            {
                Logger.LogCritical("Environment readiness checks failed for Action Extension {0}", actionIdentifier);
                return null;
            }
            return instance;
        }

        public ActionExtension EmitConfiguredActionExtension(string extensionIdentifier, Artifact artifactRoot)
        {
            if (!_actionExtensionMap.ContainsKey(extensionIdentifier))
            {
                Logger.LogCritical("Requested Emit for Action Identifier '{0}' which does not exist", extensionIdentifier);
                return null;
            }

            return (ActionExtension)Activator.CreateInstance(_actionExtensionMap[extensionIdentifier]);
        }

        private ReactionExtension GetReactionInstance(Type type)
        {
            ExtensionAttribute metadata = type.GetCustomAttribute<ExtensionAttribute>();
            ReactionExtension instance = (ReactionExtension)Activator.CreateInstance(type);
            instance.Configure();

            if (!instance.ValidateEnvironmentReadiness())
            {
                Logger.LogCritical("Environment readiness checks failed for Reaction Extension {0}", metadata.ExtensionIdentifier);
                return null;
            }

            Logger.LogInformation("Running Initialize routine on Reaction Extension {0}", metadata.ExtensionIdentifier);
            instance.Initialize();
            Logger.LogInformation("Completed Initialize routine on Reaction Extension {0}", metadata.ExtensionIdentifier);
            return instance;
        }

        public List<ReactionExtension> GetReactionExtensionsTriggeredBy(Artifact triggeringArtifact, ArtifactTrigger trigger)
        {
            return _reactionExtensionMap
.Where(m => m.Key is ArtifactTriggerDescriptor &&
((ArtifactTriggerDescriptor)m.Key).Trigger == trigger &&
m.Key.PathMatches(triggeringArtifact,
((ArtifactTriggerDescriptor)m.Key).ArtifactPath))
.SelectMany(m => m.Value.Select(v => GetReactionInstance(v)))
.ToList();
        }

        public List<ReactionExtension> GetReactionExtensionsTriggeredBy(Extension triggeringExtension, ExtensionConditionTrigger trigger)
        {
            return _reactionExtensionMap
.Where(m => m.Key is ExtensionTriggerDescriptor &&
((ExtensionTriggerDescriptor)m.Key).Trigger == trigger &&
((ExtensionTriggerDescriptor)m.Key).ExtensionIdentifier ==
triggeringExtension.GetType().GetCustomAttribute<ExtensionAttribute>().ExtensionIdentifier)
.SelectMany(m => m.Value.Select(v => GetReactionInstance(v)))
.ToList();
        }

        public List<ReactionExtension> GetReactionExtensionsTriggeredBy(SystemEvents triggeringEvent)
        {
            return _reactionExtensionMap
.Where(m => m.Key is ExtensionTriggerDescriptor &&
((SystemEventTriggerDescriptor)m.Key).SystemEvent == triggeringEvent)
.SelectMany(m => m.Value.Select(v => GetReactionInstance(v)))
.ToList();
        }
    }
}
