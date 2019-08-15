using SMACD.Artifacts;
using SMACD.Artifacts.Data;
using SMACD.SDK;
using System.IO;
using System.Linq;

namespace SMACD.ScanEngine
{
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
        /// Scanning session
        /// </summary>
        /// <param name="rootArtifact">Root artifact</param>
        public Session(RootArtifact rootArtifact = null)
        {
            ExtensionToolbox.Instance.LoadExtensionLibrariesFromPath(
                Path.Combine(Directory.GetCurrentDirectory(), "Plugins"),
                "SMACD.Plugins.*.dll");

            DataArtifact.ResolveType = new System.Func<string, System.Type>(s => ExtensionToolbox.ResolveType(s));

            if (rootArtifact == null)
            {
                rootArtifact = new RootArtifact();
            }

            Artifacts = rootArtifact;
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
