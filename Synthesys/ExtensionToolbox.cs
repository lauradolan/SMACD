using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.Logging;
using SMACD.Artifacts;
using Synthesys.SDK.Attributes;
using Synthesys.SDK.Extensions;
using Synthesys.SDK.Triggers;

namespace Synthesys
{
    public class ExtensionToolbox
    {
        private static readonly Lazy<ExtensionToolbox> _instance =
            new Lazy<ExtensionToolbox>(() => new ExtensionToolbox());

        private readonly List<ExtensionLibrary> _extensionLibraries = new List<ExtensionLibrary>();

        /// <summary>
        ///     Extension toolbox singleton instance
        /// </summary>
        public static ExtensionToolbox Instance => _instance.Value;

        private Dictionary<string, Type> _actionExtensionMap =>
            _extensionLibraries.SelectMany(l => l.ActionExtensions)
                .ToDictionary(k => k.Key, v => v.Value);

        private Dictionary<TriggerDescriptor, List<Type>> _reactionExtensionMap =>
            _extensionLibraries.SelectMany(l => l.ReactionExtensions)
                .ToDictionary(k => k.Key, v => v.Value);

        /// <summary>
        ///     Extension libraries loaded in this toolbox
        /// </summary>
        public IReadOnlyList<ExtensionLibrary> ExtensionLibraries => _extensionLibraries.AsReadOnly();

        protected ILogger Logger { get; } = Global.LogFactory.CreateLogger("TaskToolbox");

        /// <summary>
        /// Resolve an ActionExtension or ReactionExtension from its ExtensionIdentifier
        /// </summary>
        /// <param name="extensionId"></param>
        /// <returns></returns>
        public Extension ResolveExtensionFromId(string extensionId)
        {
            if (_actionExtensionMap.ContainsKey(extensionId))
                return EmitAction(extensionId);
            else
                return GetReactionInstance(_extensionLibraries.Select(l => l.ProvidedTypes.FirstOrDefault(t =>
                    t.GetCustomAttribute<ExtensionAttribute>()?.ExtensionIdentifier == extensionId)).FirstOrDefault(i => i != null));
        }

        /// <summary>
        ///     Resolve Type against Types provided by loaded libraries
        /// </summary>
        /// <param name="typeName">Type to resolve</param>
        /// <returns></returns>
        public static Type ResolveType(string typeName)
        {
            return Instance.ExtensionLibraries.Select(l => l.Assembly.GetType(typeName)).FirstOrDefault(t => t != null);
            //return Instance.ExtensionLibraries.SelectMany(l => l.ProvidedTypes).FirstOrDefault(t => t.FullName == typeName);
        }

        /// <summary>
        ///     Load libraries from a directory
        /// </summary>
        /// <param name="path">Search path</param>
        /// <param name="filePattern">File mask patterm (my.plugins.*.dll)</param>
        /// <param name="recursive">Search inside directories below the given path</param>
        public void LoadExtensionLibrariesFromPath(string path, string filePattern, bool recursive = false)
        {
            Directory.GetFiles(path, filePattern,
                    recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).ToList()
                .ForEach(f => LoadExtensionLibrary(f));
        }

        /// <summary>
        ///     Load a single extension library from a path
        /// </summary>
        /// <param name="path">Extension library path</param>
        public void LoadExtensionLibrary(string path)
        {
            _extensionLibraries.Add(new ExtensionLibrary(path));
        }

        /// <summary>
        ///     Get the metadata associated with a given Extension
        /// </summary>
        /// <param name="identifier">Action or Reaction Extension ID</param>
        /// <returns></returns>
        public ExtensionAttribute GetExtensionMetadata(string identifier)
        {
            return GetExtensionMetadata()
                .FirstOrDefault(e => e.ExtensionIdentifier == identifier);
        }

        /// <summary>
        ///     Get the metadata for all loaded Extensions
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ExtensionAttribute> GetExtensionMetadata()
        {
            return _actionExtensionMap.Select(a => a.Value).Union(
                    _reactionExtensionMap.SelectMany(r => r.Value))
                .Select(t => t.GetCustomAttribute<ExtensionAttribute>());
        }

        /// <summary>
        ///     Emit a configured ActionExtension
        /// </summary>
        /// <param name="actionIdentifier">Action identifier</param>
        /// <param name="options">Options for Action</param>
        /// <param name="artifactRoot">Artifact root</param>
        /// <returns></returns>
        public ActionExtension EmitConfiguredAction(string actionIdentifier, Dictionary<string, string> options,
            Artifact artifactRoot)
        {
            var instance = EmitAction(actionIdentifier).Configure(artifactRoot, options);
            if (!instance.ValidateEnvironmentReadiness())
            {
                Logger.LogCritical("Environment readiness checks failed for Action Extension {0}", actionIdentifier);
                return null;
            }

            return instance as ActionExtension;
        }

        /// <summary>
        ///     Emit an ActionExtension
        /// </summary>
        /// <param name="extensionIdentifier">Action identifier</param>
        /// <returns></returns>
        public ActionExtension EmitAction(string extensionIdentifier)
        {
            if (!_actionExtensionMap.ContainsKey(extensionIdentifier))
            {
                Logger.LogCritical("Requested Emit for Action Identifier '{0}' which does not exist",
                    extensionIdentifier);
                return null;
            }

            return (ActionExtension) Activator.CreateInstance(_actionExtensionMap[extensionIdentifier]);
        }

        private ReactionExtension GetReactionInstance(Type type)
        {
            var metadata = type.GetCustomAttribute<ExtensionAttribute>();
            var instance = (ReactionExtension) Activator.CreateInstance(type);
            instance.Configure();

            if (!instance.ValidateEnvironmentReadiness())
            {
                Logger.LogCritical("Environment readiness checks failed for Reaction Extension {0}",
                    metadata.ExtensionIdentifier);
                return null;
            }

            Logger.LogInformation("Running Initialize routine on Reaction Extension {0}", metadata.ExtensionIdentifier);
            instance.Initialize();
            Logger.LogInformation("Completed Initialize routine on Reaction Extension {0}",
                metadata.ExtensionIdentifier);
            return instance;
        }

        /// <summary>
        ///     Get a list of ReactionExtensions triggered by an action performed on a given Artifact
        /// </summary>
        /// <param name="triggeringArtifact">Artifact causing the trigger</param>
        /// <param name="trigger">Trigger action type</param>
        /// <returns></returns>
        public List<ReactionExtension> GetReactionExtensionsTriggeredBy(Artifact triggeringArtifact,
            ArtifactTrigger trigger)
        {
            return _reactionExtensionMap
                .Where(m => m.Key is ArtifactTriggerDescriptor &&
                            ((ArtifactTriggerDescriptor) m.Key).Trigger == trigger &&
                            TriggerDescriptor.PathMatches(triggeringArtifact,
                                ((ArtifactTriggerDescriptor) m.Key).ArtifactPath))
                .SelectMany(m => m.Value.Select(v => GetReactionInstance(v)))
                .ToList();
        }

        /// <summary>
        ///     Get a list of ReactionExtensions triggered by an extension being run
        /// </summary>
        /// <param name="triggeringExtension">Extension causing the trigger</param>
        /// <param name="trigger">Trigger execution state condition</param>
        /// <returns></returns>
        public List<ReactionExtension> GetReactionExtensionsTriggeredBy(Extension triggeringExtension,
            ExtensionConditionTrigger trigger)
        {
            return _reactionExtensionMap
                .Where(m => m.Key is ExtensionTriggerDescriptor &&
                            ((ExtensionTriggerDescriptor) m.Key).Trigger == trigger &&
                            ((ExtensionTriggerDescriptor) m.Key).ExtensionIdentifier ==
                            triggeringExtension.GetType().GetCustomAttribute<ExtensionAttribute>().ExtensionIdentifier)
                .SelectMany(m => m.Value.Select(v => GetReactionInstance(v)))
                .ToList();
        }


        /// <summary>
        ///     Get a list of ReactionExtensions triggered by a system event
        /// </summary>
        /// <param name="triggeringEvent">System event causing the trigger</param>
        /// <returns></returns>
        public List<ReactionExtension> GetReactionExtensionsTriggeredBy(SystemEvents triggeringEvent)
        {
            return _reactionExtensionMap
                .Where(m => m.Key is ExtensionTriggerDescriptor &&
                            ((SystemEventTriggerDescriptor) m.Key).SystemEvent == triggeringEvent)
                .SelectMany(m => m.Value.Select(v => GetReactionInstance(v)))
                .ToList();
        }
    }
}