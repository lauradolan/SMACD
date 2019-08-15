﻿using Polenter.Serialization;
using SMACD.Artifacts;
using SMACD.Artifacts.Data;
using SMACD.SDK;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace SMACD.ScanEngine
{
    public class ExportableSession
    {
        public RootArtifact Artifacts { get; set; }
        public List<ExtensionReport> Reports { get; set; } = new List<ExtensionReport>();

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
        /// Create a Session from an exported Session
        /// </summary>
        /// <param name="existingSession">Exported Session stream</param>
        public Session(Stream existingSession)
        {
            using (var decompressor = new DeflateStream(existingSession, CompressionMode.Decompress, true))
            {
                var result = (ExportableSession)new SharpSerializer(new SharpSerializerXmlSettings()
                {
                    AdvancedSettings = new Polenter.Serialization.Core.AdvancedSharpSerializerXmlSettings()
                    {
                        TypeNameConverter = new CustomTypeConverter()
                    }
                }).Deserialize(decompressor);
                
                Artifacts = result.Artifacts;
                Reports = result.Reports;

                Artifacts.Connect();
            }
        }

        /// <summary>
        /// Create a new Session
        /// </summary>
        public Session()
        {
            DataArtifact.ResolveType = new System.Func<string, System.Type>(s => ExtensionToolbox.ResolveType(s));

            if (Artifacts == null) Artifacts = new RootArtifact();
            BindArtifactTriggers();

            Tasks = new TaskToolbox(
                (descriptor, id, opts, root) =>
                {
                    SDK.Extensions.ActionExtension action = ExtensionToolbox.Instance.EmitAction(id, opts, root);
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
                    System.Collections.Generic.List<SDK.Extensions.ReactionExtension> action = ExtensionToolbox.Instance.GetReactionExtensionsTriggeredBy(ext, trigger);
                    if (action is ICanQueueTasks)
                    {
                        ((ICanQueueTasks)action).Tasks = Tasks;
                    }

                    if (action is IUnderstandProjectInformation)
                    {
                        ((IUnderstandProjectInformation)action).ProjectPointer = descriptor.ProjectPointer;
                    }

                    return action;
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
                new SharpSerializer(new SharpSerializerXmlSettings()
                {
                    AdvancedSettings = new Polenter.Serialization.Core.AdvancedSharpSerializerXmlSettings()
                    {
                        TypeNameConverter = new CustomTypeConverter()
                    }
                }).Serialize(new ExportableSession()
                {
                    Artifacts = this.Artifacts,
                    Reports = this.Reports
                }, compressor);
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
