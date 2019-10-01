﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using SMACD.Artifacts;
using Synthesys.SDK;
using Synthesys.SDK.Capabilities;
using Synthesys.SDK.Extensions;
using Synthesys.SDK.Triggers;

namespace Synthesys
{
    public class ExportableSession
    {
        public RootArtifact Artifacts { get; set; }
        public List<string> SerializedReports { get; set; } = new List<string>();
        public string ServiceMapYaml { get; set; }
    }

    public class Session
    {
        public static ExportableSession DecompressRawExportableSession(Stream existingSession)
        {
            using (var decompressor = new DeflateStream(existingSession, CompressionMode.Decompress, true))
            {
                var ms = new MemoryStream();
                decompressor.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);

                var result = JsonConvert.DeserializeObject<ExportableSession>(
                    Encoding.Unicode.GetString(ms.ToArray()),
                    new JsonSerializerSettings
                    {
                        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                        TypeNameHandling = TypeNameHandling.All,
                        SerializationBinder = new AggressiveTypeResolutionBinder()
                    });

                return result;
            }
        }

        /// <summary>
        ///     Create a Session from an exported Session
        /// </summary>
        /// <param name="existingSession">Exported Session stream</param>
        public Session(Stream existingSession)
        {
            var result = DecompressRawExportableSession(existingSession);

            Artifacts = result.Artifacts;
            try
            {
                Reports = result.SerializedReports.Select(r => ExtensionReport.Deserialize(r)).ToList();
            }
            catch (Exception ex)
            {
            }

            ServiceMapYaml = result.ServiceMapYaml;

            Artifacts.Connect();
        }

        /// <summary>
        ///     Create a new Session
        /// </summary>
        public Session()
        {
            if (Artifacts == null) Artifacts = new RootArtifact();
            BindArtifactTriggers();

            Tasks = new TaskToolbox(
                (descriptor, id, opts, root) =>
                {
                    if (!ExtensionToolbox.Instance.ExtensionLibraries.Any(l => l.ActionExtensions.Any(e => e.Key == id))
                    )
                        return null;

                    var action = ExtensionToolbox.Instance.EmitConfiguredAction(id, opts, root);
                    if (action is ICanQueueTasks) ((ICanQueueTasks) action).Tasks = Tasks;

                    if (action is IUnderstandProjectInformation)
                        ((IUnderstandProjectInformation) action).ProjectPointer = descriptor.ProjectPointer;

                    return action;
                },
                (descriptor, ext, trigger) =>
                {
                    var reactions = new List<ReactionExtension>();
                    if (ext == null) return reactions;

                    reactions.AddRange(ExtensionToolbox.Instance.GetReactionExtensionsTriggeredBy(ext, trigger));

                    foreach (var reaction in reactions)
                    {
                        // todo: reaction options?
                        var configuredReaction = reaction.Configure(descriptor.ArtifactRoot, new Dictionary<string, string>()) as ReactionExtension;

                        if (configuredReaction is ICanQueueTasks) ((ICanQueueTasks)configuredReaction).Tasks = Tasks;
                        if (configuredReaction is IUnderstandProjectInformation)
                            ((IUnderstandProjectInformation)configuredReaction).ProjectPointer = descriptor.ProjectPointer;
                    }

                    return reactions;
                });
        }

        /// <summary>
        ///     Task queue
        /// </summary>
        public TaskToolbox Tasks { get; }

        /// <summary>
        ///     Artifact tree root
        /// </summary>
        public RootArtifact Artifacts { get; }

        /// <summary>
        ///     Reports which have been generated by Extensions in this Session
        /// </summary>
        public List<ExtensionReport> Reports { get; } = new List<ExtensionReport>();

        /// <summary>
        ///     Service Map YAML generating this Session
        /// </summary>
        public string ServiceMapYaml { get; set; }

        /// <summary>
        ///     Export the Session's reports and Artifacts to a Stream
        /// </summary>
        /// <param name="data">Stream to contain Session data</param>
        public void Export(Stream data)
        {
            using (var compressor = new DeflateStream(data, CompressionMode.Compress))
            {
                Artifacts.Disconnect();

                var serializedReports = Reports.Select(r => r.Serialize()).ToList();
                var str = JsonConvert.SerializeObject(new ExportableSession
                {
                    Artifacts = Artifacts,
                    SerializedReports = serializedReports,
                    ServiceMapYaml = ServiceMapYaml
                }, new JsonSerializerSettings
                {
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                    TypeNameHandling = TypeNameHandling.All,
                    SerializationBinder = new AggressiveTypeResolutionBinder()
                });
                compressor.Write(Encoding.Unicode.GetBytes(str));

                Artifacts.Connect();
            }
        }

        private void BindArtifactTriggers()
        {
            Artifacts.ArtifactChanged += (artifact, path) =>
            {
                var triggered =
                    ExtensionToolbox.Instance.GetReactionExtensionsTriggeredBy(artifact, ArtifactTrigger.IsUpdated);
                foreach (var item in triggered)
                    item.React(TriggerDescriptor.ArtifactTrigger(
                        string.Join("|;|", path.Select(p => p.Identifier)),
                        ArtifactTrigger.IsUpdated));
            };
            Artifacts.ArtifactChildAdded += (artifact, path) =>
            {
                artifact = artifact.Parent; // this event returns the child
                var triggered =
                    ExtensionToolbox.Instance.GetReactionExtensionsTriggeredBy(artifact, ArtifactTrigger.AddsChild);
                foreach (var item in triggered)
                    item.React(TriggerDescriptor.ArtifactTrigger(
                        string.Join("|;|", path.Skip(1).Select(p => p.Identifier)),
                        ArtifactTrigger.AddsChild)); // note Skip(1), same reason as above
            };
            Artifacts.ArtifactCreated += (artifact, path) =>
            {
                var triggered =
                    ExtensionToolbox.Instance.GetReactionExtensionsTriggeredBy(artifact, ArtifactTrigger.IsCreated);
                foreach (var item in triggered)
                    item.React(TriggerDescriptor.ArtifactTrigger(
                        string.Join("|;|", path.Select(p => p.Identifier)),
                        ArtifactTrigger.IsCreated));
            };
        }
    }
}