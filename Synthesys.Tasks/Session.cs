﻿using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SMACD.AppTree;
using Synthesys.SDK;
using Synthesys.SDK.Extensions;
using Synthesys.SDK.Triggers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace Synthesys.Tasks
{
    public class Session
    {
        /// <summary>
        ///     Create a Session from a previously exported Session
        /// </summary>
        /// <param name="existingSession">Previously exported Session</param>
        /// <returns>Imported and linked Session</returns>
        public static Session Import(Stream existingSession)
        {
            using (DeflateStream decompressor = new DeflateStream(existingSession, CompressionMode.Decompress, true))
            {
                MemoryStream ms = new MemoryStream();
                decompressor.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);

                Session result = JsonConvert.DeserializeObject<Session>(
                    Encoding.Unicode.GetString(ms.ToArray()),
                    new JsonSerializerSettings
                    {
                        TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                        TypeNameHandling = TypeNameHandling.All,
                        SerializationBinder = new AggressiveTypeResolutionBinder(),
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    });

                result.Artifacts.Connect();
                result.BindArtifactTriggers();

                return result;
            }
        }

        /// <summary>
        ///     Create a new Session
        /// </summary>
        public Session()
        {
            if (Artifacts == null)
            {
                Artifacts = new RootNode();
            }

            BindArtifactTriggers();
        }

        /// <summary>
        ///     Task queue
        /// </summary>
        [JsonIgnore]
        public TaskToolbox Tasks { get; } = new TaskToolbox();

        /// <summary>
        ///     Artifact tree root
        /// </summary>
        public RootNode Artifacts { get; }

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
            using (DeflateStream compressor = new DeflateStream(data, CompressionMode.Compress))
            {
                string str = JsonConvert.SerializeObject(this, new JsonSerializerSettings
                {
                    TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
                    TypeNameHandling = TypeNameHandling.All,
                    SerializationBinder = new AggressiveTypeResolutionBinder(),
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
                compressor.Write(Encoding.Unicode.GetBytes(str));
            }
        }

        private ILogger Logger { get; set; } = Global.LogFactory.CreateLogger("Session");
        private void BindArtifactTriggers()
        {
            Artifacts.ArtifactChanged += artifact =>
            {
                var trigger = TriggerDescriptor.ArtifactTrigger(
                        artifact,
                        AppTreeNodeEvents.IsUpdated);
                EngageTriggeredReactions(trigger, ExtensionToolbox.Instance.GetReactionExtensionsTriggeredBy(artifact, AppTreeNodeEvents.IsUpdated));
            };
            Artifacts.ArtifactChildAdded += artifact =>
            {
                artifact = artifact.Parent; // this event returns the child
                var trigger = TriggerDescriptor.ArtifactTrigger(
                        artifact,
                        AppTreeNodeEvents.AddsChild);
                EngageTriggeredReactions(trigger, ExtensionToolbox.Instance.GetReactionExtensionsTriggeredBy(artifact, AppTreeNodeEvents.AddsChild));
            };
            Artifacts.ArtifactCreated += artifact =>
            {
                var trigger = TriggerDescriptor.ArtifactTrigger(
                        artifact,
                        AppTreeNodeEvents.IsCreated);
                EngageTriggeredReactions(trigger, ExtensionToolbox.Instance.GetReactionExtensionsTriggeredBy(artifact, AppTreeNodeEvents.IsCreated));
            };
        }

        private void EngageTriggeredReactions(TriggerDescriptor trigger, List<ReactionExtension> extensions)
        {
            if (extensions.Any())
                Logger.LogTrace("Processing {0} ReactionExtensions triggered by {1}", extensions.Count, trigger);
            foreach (SDK.Extensions.ReactionExtension extension in extensions)
            {
                Logger.LogTrace("Processing Reaction {0}", extension.Metadata.ExtensionIdentifier);
                Reports.Add(extension.React(trigger));
            }
        }
    }
}