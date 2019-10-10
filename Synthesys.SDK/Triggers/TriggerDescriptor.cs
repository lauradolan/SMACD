using SMACD.AppTree;

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
        /// <param name="nodePath">Node path</param>
        /// <param name="trigger">Trigger operation</param>
        /// <returns></returns>
        public static ArtifactTriggerDescriptor ArtifactTrigger(string nodePath, AppTreeNodeEvents trigger)
        {
            return new ArtifactTriggerDescriptor(nodePath, trigger);
        }

        /// <summary>
        ///     Create an artifact-based trigger
        /// </summary>
        /// <param name="node">Node instance</param>
        /// <param name="trigger">Trigger operation</param>
        /// <returns></returns>
        public static ArtifactTriggerDescriptor ArtifactTrigger(AppTreeNode node, AppTreeNodeEvents trigger)
        {
            return new ArtifactTriggerDescriptor(node, trigger);
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
        /// <param name="node">Artifact</param>
        /// <returns></returns>
        public static string GeneratePath(AppTreeNode node)
        {
            return node.GetUUIDPath();
        }
    }
}