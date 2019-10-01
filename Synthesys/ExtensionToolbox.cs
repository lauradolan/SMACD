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
        ///     Resolve an ActionExtension or ReactionExtension from its Extension identifier
        /// </summary>
        /// <param name="extensionId">Extension identifier</param>
        /// <returns></returns>
        public Extension ResolveExtensionFromId(string extensionId)
        {
            if (_actionExtensionMap.ContainsKey(extensionId))
                return EmitAction(extensionId);
            else
                return EmitReaction(_extensionLibraries.Select(l => l.ProvidedTypes.FirstOrDefault(t =>
                    t.GetCustomAttribute<ExtensionAttribute>()?.ExtensionIdentifier == extensionId)).FirstOrDefault(i => i != null));
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
                .SelectMany(m => m.Value.Select(v => EmitReaction(v)))
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
                .SelectMany(m => m.Value.Select(v => EmitReaction(v)))
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
                .SelectMany(m => m.Value.Select(v => EmitReaction(v)))
                .ToList();
        }

        /// <summary>
        ///     Emit a non-contextualized instance of a ReactionExtension (expects Type to exist and inherit from ReactionExtension)
        /// </summary>
        /// <param name="type">ReactionExtension Type</param>
        /// <returns></returns>
        private ReactionExtension EmitReaction(Type type)
        {
            return (ReactionExtension)Activator.CreateInstance(type);
        }
    }
}