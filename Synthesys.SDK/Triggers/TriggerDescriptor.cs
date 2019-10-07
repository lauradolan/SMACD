using SMACD.Artifacts;

namespace Synthesys.SDK.Triggers
{
    /// <summary>
    ///     Describes a wrapper around an event and its details, to pass to ReactionExtensions
    /// </summary>
    public class TriggerDescriptor
    {
        /// <summary>
        ///     Describes a wrapper around an event and its details, to pass to ReactionExtensions
        /// </summary>
        protected TriggerDescriptor()
        {
        }

        /// <summary>
        ///     Create an artifact-based trigger
        /// </summary>
        /// <param name="artifactPath">Artifact path</param>
        /// <param name="trigger">Trigger operation</param>
        /// <returns></returns>
        public static ArtifactTriggerDescriptor ArtifactTrigger(string artifactPath, ArtifactTrigger trigger)
        {
            return new ArtifactTriggerDescriptor(artifactPath, trigger);
        }

        /// <summary>
        ///     Create an artifact-based trigger
        /// </summary>
        /// <param name="artifact">Artifact instance</param>
        /// <param name="trigger">Trigger operation</param>
        /// <returns></returns>
        public static ArtifactTriggerDescriptor ArtifactTrigger(Artifact artifact, ArtifactTrigger trigger)
        {
            return new ArtifactTriggerDescriptor(artifact, trigger);
        }

        /// <summary>
        ///     Create an extension-based trigger
        /// </summary>
        /// <param name="extensionIdentifier">Extension identifier</param>
        /// <param name="trigger">Extension execution condition</param>
        /// <returns></returns>
        public static ExtensionTriggerDescriptor ExtensionTrigger(string extensionIdentifier,
            ExtensionConditionTrigger trigger)
        {
            return new ExtensionTriggerDescriptor(extensionIdentifier, trigger);
        }

        /// <summary>
        ///     Create a trigger activated by a SystemEvent
        /// </summary>
        /// <param name="trigger">System event</param>
        /// <returns></returns>
        public static SystemEventTriggerDescriptor SystemEventTrigger(SystemEvents trigger)
        {
            return new SystemEventTriggerDescriptor(trigger);
        }

        /// <summary>
        ///     Generate the path for a given artifact
        /// </summary>
        /// <param name="artifact">Artifact</param>
        /// <returns></returns>
        public static string GeneratePath(Artifact artifact)
        {
            return artifact.GetUUIDPathToRoot();
        }
    }
}