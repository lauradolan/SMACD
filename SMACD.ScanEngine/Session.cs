﻿using Newtonsoft.Json;
using SMACD.Artifacts;
using SMACD.Artifacts.Data;
using SMACD.SDK;
using SMACD.SDK.Capabilities;
using SMACD.SDK.Triggers;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace SMACD.ScanEngine
{
    public class ExportableSession
    {
        public RootArtifact Artifacts { get; set; }
        public List<string> SerializedReports { get; set; } = new List<string>();
        public string ServiceMapYaml { get; set; }
    }

    public class Session
    {
        /// <summary>
        /// Task queue
        /// </summary>
        public TaskToolbox Tasks { get; }

        /// <summary>
        /// Artifact tree root
        /// </summary>
        public RootArtifact Artifacts { get; }

        /// <summary>
        /// Reports which have been generated by Extensions in this Session
        /// </summary>
        public List<ExtensionReport> Reports { get; } = new List<ExtensionReport>();

        /// <summary>
        /// Service Map YAML generating this Session
        /// </summary>
        public string ServiceMapYaml { get; set; }

        /// <summary>
        /// Create a Session from an exported Session
        /// </summary>
        /// <param name="existingSession">Exported Session stream</param>
        public Session(Stream existingSession)
        {
            using (var decompressor = new DeflateStream(existingSession, CompressionMode.Decompress, true))
            {
                var ms = new MemoryStream();
                decompressor.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);

                var result = Newtonsoft.Json.JsonConvert.DeserializeObject<ExportableSession>(
                    UnicodeEncoding.Unicode.GetString(ms.ToArray()),
                    new JsonSerializerSettings()
                    {
                        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                        TypeNameHandling = TypeNameHandling.All,
                        SerializationBinder = new AggressiveTypeResolutionBinder()
                    });

                ms.Seek(0, SeekOrigin.Begin);
                var txt = System.Text.UnicodeEncoding.Unicode.GetString(ms.ToArray());

                Artifacts = result.Artifacts;
                try
                {
                    Reports = result.SerializedReports.Select(r => ExtensionReport.Deserialize(r)).ToList();
                }
                catch (Exception ex) { }
                ServiceMapYaml = result.ServiceMapYaml;

                Artifacts.Connect();
            }
        }

        /// <summary>
        /// Create a new Session
        /// </summary>
        public Session()
        {
            if (Artifacts == null) Artifacts = new RootArtifact();
            BindArtifactTriggers();

            Tasks = new TaskToolbox(
                (descriptor, id, opts, root) =>
                {
                    if (!ExtensionToolbox.Instance.ExtensionLibraries.Any(l => l.ActionExtensions.Any(e => e.Key == id)))
                        return null;

                    SDK.Extensions.ActionExtension action = ExtensionToolbox.Instance.EmitConfiguredAction(id, opts, root);
                    if (action is ICanQueueTasks)
                    {
                        ((ICanQueueTasks)action).Tasks = Tasks;
                    }

                    if (action is IUnderstandProjectInformation)
                    {
                        ((IUnderstandProjectInformation)action).ProjectPointer = descriptor.ProjectPointer;
                    }

                    return action;
                },
                (descriptor, ext, trigger) =>
                {
                    var reactions = new List<SDK.Extensions.ReactionExtension>();
                    if (ext == null) return reactions;

                    reactions.AddRange(ExtensionToolbox.Instance.GetReactionExtensionsTriggeredBy(ext, trigger));

                    foreach (var reaction in reactions)
                    {
                        if (reaction is ICanQueueTasks)
                        {
                            ((ICanQueueTasks)reaction).Tasks = Tasks;
                        }

                        if (reaction is IUnderstandProjectInformation)
                        {
                            ((IUnderstandProjectInformation)reaction).ProjectPointer = descriptor.ProjectPointer;
                        }
                    }
                    return reactions;
                });
        }

        /// <summary>
        /// Export the Session's reports and Artifacts to a Stream
        /// </summary>
        /// <param name="data">Stream to contain Session data</param>
        public void Export(Stream data)
        {
            using (var compressor = new DeflateStream(data, CompressionMode.Compress))
            {
                Artifacts.Disconnect();

                var serializedReports = Reports.Select(r => r.Serialize()).ToList();
                var str = Newtonsoft.Json.JsonConvert.SerializeObject(new ExportableSession()
                {
                    Artifacts = this.Artifacts,
                    SerializedReports = serializedReports,
                    ServiceMapYaml = this.ServiceMapYaml
                }, new JsonSerializerSettings() {
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                    TypeNameHandling = TypeNameHandling.All,
                    SerializationBinder = new AggressiveTypeResolutionBinder()
                });
                compressor.Write(UnicodeEncoding.Unicode.GetBytes(str));

                Artifacts.Connect();
            }
        }

        private void BindArtifactTriggers()
        {
            Artifacts.ArtifactChanged += (artifact, path) =>
            {
                System.Collections.Generic.List<SDK.Extensions.ReactionExtension> triggered = ExtensionToolbox.Instance.GetReactionExtensionsTriggeredBy(artifact, ArtifactTrigger.IsUpdated);
                foreach (SDK.Extensions.ReactionExtension item in triggered)
                {
                    item.React(TriggerDescriptor.ArtifactTrigger(
                        string.Join("|;|", path.Select(p => p.Identifier)),
                        ArtifactTrigger.IsUpdated));
                }
            };
            Artifacts.ArtifactChildAdded += (artifact, path) =>
            {
                artifact = artifact.Parent; // this event returns the child
                System.Collections.Generic.List<SDK.Extensions.ReactionExtension> triggered = ExtensionToolbox.Instance.GetReactionExtensionsTriggeredBy(artifact, ArtifactTrigger.AddsChild);
                foreach (SDK.Extensions.ReactionExtension item in triggered)
                {
                    item.React(TriggerDescriptor.ArtifactTrigger(
                        string.Join("|;|", path.Skip(1).Select(p => p.Identifier)),
                        ArtifactTrigger.AddsChild)); // note Skip(1), same reason as above
                }
            };
            Artifacts.ArtifactCreated += (artifact, path) =>
            {
                System.Collections.Generic.List<SDK.Extensions.ReactionExtension> triggered = ExtensionToolbox.Instance.GetReactionExtensionsTriggeredBy(artifact, ArtifactTrigger.IsCreated);
                foreach (SDK.Extensions.ReactionExtension item in triggered)
                {
                    item.React(TriggerDescriptor.ArtifactTrigger(
                        string.Join("|;|", path.Select(p => p.Identifier)),
                        ArtifactTrigger.IsCreated));
                }
            };
        }
    }
}
